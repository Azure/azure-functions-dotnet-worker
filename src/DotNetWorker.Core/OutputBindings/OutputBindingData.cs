// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type representing an output binding entry.
    /// </summary>
    public abstract class OutputBindingData<T>
    {
        /// <summary>
        /// Gets the type of the binding entry.
        /// Ex: "http","queue" etc.
        /// </summary>
        public abstract string BindingType { get; }

        /// <summary>
        /// Gets the name of the output binding entry.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets or sets the value of the output binding entry.
        /// </summary>
        public abstract T? Value { get; set; }
    }
}
