// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using IAsyncPageable = System.Collections.Generic.IAsyncEnumerable<System.Collections.IEnumerable>;

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
        /// <param name="pageableFactory">Factory for producing a pageable given the element type.</param>
        /// <param name="collectionType">The collection type to bind to.</param>
        /// <returns>The instantiated and populated collection.</returns>
        public static async Task<object> BindCollectionAsync(Func<Type, IAsyncPageable> pageableFactory, Type collectionType)
        {
            if (pageableFactory is null)
            {
                throw new ArgumentNullException(nameof(pageableFactory));
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
            if (collectionType.IsConcreteType())
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
                await BindCollectionAsync(pageableFactory(elementType!), collection);
                IList list = (IList)collection;
                Array arrayResult = Array.CreateInstance(elementType!, list.Count);
                list.CopyTo(arrayResult, 0);
                return arrayResult;
            }
            else
            {
                throw new ArgumentException($"Collection type '{collectionType}' is not supported for parameter binding.", nameof(collectionType));
            }

            await BindCollectionAsync(pageableFactory(elementType!), collection);
            return collection;
        }

        /// <summary>
        /// Binds to a collection.
        /// </summary>
        /// <param name="pageable">The pageable containing the items to populate the collection with.</param>
        /// <param name="collection">The collection to populate.</param>
        /// <returns>A task that completes when the collection has been populated.</returns>
        public static async Task BindCollectionAsync(IAsyncPageable pageable, object collection)
        {
            if (pageable is null)
            {
                throw new ArgumentNullException(nameof(pageable));
            }

            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            Action<object, object> add = GetAddMethod(collection);
            await foreach (IEnumerable page in pageable)
            {
                foreach (object item in page)
                {
                    add(collection, item);
                }
            }
        }

        private static bool IsListInterface(Type type)
        {
            return type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(IList<>)
                || type.GetGenericTypeDefinition() == typeof(ICollection<>)
                || type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private static Action<object, object> GetAddMethod(object collection)
        {
            if (collection is IList list)
            {
                return (c, e) => list.Add(e);
            }

            MethodInfo method = collection.GetType().GetMethod("Add", DeclaredOnlyLookup)
                ?? throw new InvalidOperationException($"Could not find an 'Add' method on collection type '{collection.GetType()}'.");
            return (c, e) => method.Invoke(c, new[] { e });
        }
    }
}
