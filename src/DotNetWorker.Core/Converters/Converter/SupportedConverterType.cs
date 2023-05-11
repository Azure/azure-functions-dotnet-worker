// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a type supported by function input conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SupportedConverterTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the input converter type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SupportedConverterTypeAttribute"/>
        /// </summary>
        /// <param name="type">Input converter type.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        public SupportedConverterTypeAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
