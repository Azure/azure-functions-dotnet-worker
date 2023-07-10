// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Specifies the fallback behavior for a converter.
    /// The default behavior is <see cref="ConverterFallbackBehavior.Allow"/>.
    /// </summary>
    public enum ConverterFallbackBehavior
    {
        /// <summary>
        /// Allows fallback to built-in converters. This is the default behavior.
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Disallows fallback to built-in converters.
        /// </summary>
        Disallow = 1,

        /// <summary>
        /// Specifies the default fallback behavior as <see cref="ConverterFallbackBehavior.Allow"/>
        /// </summary>
        Default = Allow
    }
}
