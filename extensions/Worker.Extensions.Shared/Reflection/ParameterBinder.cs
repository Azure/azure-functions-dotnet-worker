// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Extensions
{
    /// <summary>
    /// Helpers for performing parameter binding.
    /// </summary>
    internal static class ParameterBinder
    {
        private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Binds to a collection type.
        /// </summary>
        /// <param name="factory">Factory for producing an enumerable given the element type.</param>
        /// <param name="collectionType">The collection type to bind to.</param>
        /// <returns>The instantiated and populated collection.</returns>
        public static async Task<object> BindCollectionAsync(Func<Type, IAsyncEnumerable<object>> factory, Type collectionType)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (collectionType is null)
            {
                throw new ArgumentNullException(nameof(collectionType));
            }

            if (!collectionType.TryGetCollectionElementType(out Type? elementType))
            {
                throw new ArgumentException($"Type '{collectionType}' is not a collection type.", nameof(collectionType));
            }

            object? collection = null;
            if (collectionType.IsConcreteType() && !collectionType.IsArray)
            {
                collection = Activator.CreateInstance(collectionType)!;
            }
            else if (IsListInterface(collectionType))
            {
                collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType!))!;
            }
            else if (collectionType.IsArray)
            {
                collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType!))!;
                await BindCollectionAsync(factory(elementType!), collection);
                IList list = (IList)collection;
                Array arrayResult = Array.CreateInstance(elementType!, list.Count);
                list.CopyTo(arrayResult, 0);
                return arrayResult;
            }
            else
            {
                throw new ArgumentException($"Collection type '{collectionType}' is not supported for parameter binding.", nameof(collectionType));
            }

            await BindCollectionAsync(factory(elementType!), collection);
            return collection;
        }

        /// <summary>
        /// Binds to a collection.
        /// </summary>
        /// <param name="pageable">The pageable containing the items to populate the collection with.</param>
        /// <param name="collection">The collection to populate.</param>
        /// <returns>A task that completes when the collection has been populated.</returns>
        public static async Task BindCollectionAsync(IAsyncEnumerable<object> pageable, object collection)
        {
            if (pageable is null)
            {
                throw new ArgumentNullException(nameof(pageable));
            }

            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            Action<object> add = GetAddMethod(collection);
            await foreach (object item in pageable)
            {
                add(item);
            }
        }

        private static bool IsListInterface(Type type)
        {
            return type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(IList<>)
                || type.GetGenericTypeDefinition() == typeof(ICollection<>)
                || type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                || type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));
        }

        private static Action<object> GetAddMethod(object collection)
        {
            if (collection is IList list)
            {
                return e => list.Add(e);
            }

            MethodInfo method = collection.GetType().GetMethod("Add", DeclaredOnlyLookup)
                ?? throw new InvalidOperationException($"Could not find an 'Add' method on collection type '{collection.GetType()}'.");
            return e => method.Invoke(collection, new[] { e });
        }
    }
}
