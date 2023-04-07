// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a type of <see cref="Type"/> to use for function input conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SupportedConverterTypesAttribute : Attribute
    {
        /// <summary>
        /// Gets the input converter types.
        /// </summary>
        public List<Type>? Types { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SupportedConverterTypesAttribute"/>
        /// </summary>
        /// <param name="types">Input converter types.</param>
        /// <exception cref="ArgumentNullException">Thrown when types is null or empty</exception>
        public SupportedConverterTypesAttribute(params Type[] types)
        {
            if (types == null || types.Length == 0)
            {
                throw new ArgumentNullException(nameof(types));
            }

            Types = new List<Type> { };

            foreach (var type in types)
            {
                Types.Add(type);
            }
        }
    }
}
