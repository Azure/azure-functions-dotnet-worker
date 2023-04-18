// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a type of <see cref="Type"/> to use for function input conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SupportedConverterTypesAttribute : Attribute
    {
        /// <summary>
        /// Gets the input converter type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Supports collection
        /// </summary>
        public bool SupportsCollection { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SupportedConverterTypesAttribute"/>
        /// </summary>
        /// <param name="type">Input converter type.</param>
        /// <param name="supportsCollection">Supports collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null or empty</exception>
        public SupportedConverterTypesAttribute(Type type, bool supportsCollection = false)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            SupportsCollection = supportsCollection;
        }
    }
}
