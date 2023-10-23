// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Extensions
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Returns true if the target type is a collection type.
        /// </summary>
        public static bool IsCollectionType(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // Edge case: string implements IEnumerable<char> and byte[] would pass the IsArray check
            if (type == typeof(string) || type == typeof(byte[]))
            {
                return false;
            }

            if (!(type.IsArray || typeof(IEnumerable).IsAssignableFrom(type)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the element type of an collection.
        /// </summary>
        public static bool TryGetCollectionElementType(this Type type, out Type? elementType)
        {
            elementType = null;

            if (!type.IsCollectionType())
            {
                return false;
            }

            // Check if the type is an array
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            // Traverse the inheritance hierarchy to find the first generic interface
            while (type is not null)
            {
                var interfaceType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType
                    || (t == typeof(IEnumerable) || t == typeof(ICollection) || t == typeof(IList)));

                if (interfaceType is not null)
                {
                    elementType = interfaceType.GetGenericArguments()[0];
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the type is concrete, and false if it is not.
        /// A concrete type is a class that allows creating an instance or an object using the new keyword.
        /// </summary>
        public static bool IsConcreteType(this Type type)
        {
            return !type.IsAbstract && !type.IsInterface;
        }
    }
}
