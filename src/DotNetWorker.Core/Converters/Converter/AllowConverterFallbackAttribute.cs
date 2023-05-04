// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that specifies if Converters fallback is allowed
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Parameter |
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Enum |
        AttributeTargets.Struct)]
    public sealed class AllowConverterFallbackAttribute : Attribute
    {
        /// <summary>
        /// Gets the value if Converter fallback is allowed.
        /// </summary>
        public bool AllowConverterFallback { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AllowConverterFallbackAttribute"/>
        /// </summary>
        /// <param name="allowConverterFallback">The value that allows converter fallback.</param>
        public AllowConverterFallbackAttribute(bool allowConverterFallback)
        {
            AllowConverterFallback = allowConverterFallback;
        }

    }
}
