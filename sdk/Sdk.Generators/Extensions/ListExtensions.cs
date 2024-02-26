// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class ListExtensions
    {
        /// <summary>
        /// If the enumerable passed in is already a list, it is returned, otherwise ToList() is invoked.
        /// </summary>
        /// <typeparam name="T">The type of element in the list.</typeparam>
        /// <param name="source">The enumerable to return as a list.</param>
        internal static List<T> AsList<T>(this IEnumerable<T>? source)
        {
            if (source == null)
            {
                return null!;
            }

            if (source is List<T> list)
            {
                return list;
            }

            return source.ToList();
        }
    }
}
