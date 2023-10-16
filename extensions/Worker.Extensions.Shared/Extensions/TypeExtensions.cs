// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;

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
            if (!type.IsCollectionType())
            {
                elementType = null;
                return false;
            }

            elementType = type.IsArray ? type.GetElementType() : type.GenericTypeArguments[0];
            return true;
        }
    }
}
