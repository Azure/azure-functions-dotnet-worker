// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a target type supported by function input conversion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SupportedTargetTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the input converter supported target type.
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SupportedTargetTypeAttribute"/>
        /// </summary>
        /// <param name="targetType">Input converter target type.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        public SupportedTargetTypeAttribute(Type targetType)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }
    }
}
