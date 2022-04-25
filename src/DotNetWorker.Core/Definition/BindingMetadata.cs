// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Contains metadata about an Azure Functions binding.
    /// </summary>
    public abstract class BindingMetadata
    {
        /// <summary>
        /// Gets the name of the binding metadata entry.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the type of the binding. For example, "httpTrigger".
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Gets the binding direction.
        /// </summary>
        public abstract BindingDirection Direction { get; }
    }
}
