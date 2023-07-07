// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that specifies if converter fallback is allowed or disallowed.
    /// Converter fallback refers to the ability to use built-in converters when custom converters
    /// cannot handle a given request.
    /// The default converter fallback behavior is <see cref="ConverterFallbackBehavior.Allow"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConverterFallbackBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Gets the value of the converter fallback behavior.
        /// </summary>
        public ConverterFallbackBehavior Behavior { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ConverterFallbackBehaviorAttribute"/>
        /// </summary>
        /// <param name="fallbackBehavior">The value to indicate if converter fallback is allowed or disallowed.</param>
        public ConverterFallbackBehaviorAttribute(ConverterFallbackBehavior fallbackBehavior)
        {
            Behavior = fallbackBehavior;
        }
    }

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
