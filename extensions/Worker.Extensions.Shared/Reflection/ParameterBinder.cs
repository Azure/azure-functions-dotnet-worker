// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Extensions
{
    /// <summary>
    /// Helpers for performing parameter binding.
    /// </summary>
    internal static class ParameterBinder
    {
        /// <summary>
        /// Read only property that contains all of the generic interfaces implemented by <see cref="System.Collections.Generic.List"/>.
        /// </summary>
        /// <remarks>This property calls ToList at the end to force the resolution of the LINQ methods that leverage deferred execution.</remarks>
        private static readonly IEnumerable<Type> validListInterfaceTypes = typeof(List<>).GetInterfaces().Where(t => t.IsGenericType).Select(t => t.GetGenericTypeDefinition()).ToList();

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

        /// <summary>
        /// A method that determines if a Type is a generic interface of the <see cref="System.Collections.Generic.List"/> concrete class.
        /// </summary>
        /// <param name="type">A generic interface type to be tested against the types available on the generic <see cref="System.Collections.Generic.List"/> type.</param>
        /// <returns>True if the type is a generic type and it's an interface of the generic <see cref="System.Collections.Generic.List"/> type otherwise false.</returns>
        private static bool IsListInterface(Type type)
        {
            return type.IsGenericType &&
                validListInterfaceTypes.Any(t => t == type.GetGenericTypeDefinition());
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
