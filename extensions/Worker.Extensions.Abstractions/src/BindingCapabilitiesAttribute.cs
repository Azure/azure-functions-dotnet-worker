// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    /// <summary>
    /// Specifies a binding type's capabilities. Intended for use on binding attribute classes only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BindingCapabilitiesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BindingCapabilitiesAttribute"/> with the specified list of capabilities.
        /// </summary>
        /// <param name="capabilities">The list of capabilities.</param>
        public BindingCapabilitiesAttribute(params string[] capabilities)
        {
            Capabilities = capabilities;
        }

        /// <summary>
        /// Lists the capabilities of a binding type.
        /// </summary>
        public string[] Capabilities { get; private set; }
    }
}
