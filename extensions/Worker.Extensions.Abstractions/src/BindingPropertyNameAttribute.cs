// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    /// <summary>
    /// Specifies the binding property name that is used when generating function metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BindingPropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BindingPropertyNameAttribute"/> with the specified property name.
        /// </summary>
        /// <param name="bindingPropertyName">The name of the property to be used when generating function metadata.</param>
        /// <exception cref="ArgumentNullException">Throws when bindingPropertyName is null.</exception>
        public BindingPropertyNameAttribute(string bindingPropertyName)
        {
            BindingPropertyName = bindingPropertyName ?? throw new ArgumentNullException(nameof(bindingPropertyName));
        }

        /// <summary>
        /// Gets the binding property name to be used for metadata generation.
        /// </summary>
        public string BindingPropertyName { get; }
    }
}
