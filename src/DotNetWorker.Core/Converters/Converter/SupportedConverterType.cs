// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a type of <see cref="Type"/> supported by function input conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SupportedConverterTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the input converter type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Boolean to indicate if collection <see cref="Type"/> is supported i.e Type[] or IEnumerable of Type.
        /// </summary>
        public bool SupportsCollection { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SupportedConverterTypeAttribute"/>
        /// </summary>
        /// <param name="type">Input converter type.</param>
        /// <param name="supportsCollection">Supports collection of param type.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        public SupportedConverterTypeAttribute(Type type, bool supportsCollection = false)
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
