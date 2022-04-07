// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type representing the input binding data.
    /// </summary>
    /// <typeparam name="T">The type of binding data value.</typeparam>
    public abstract class InputBindingData<T>
    {
        /// <summary>
        /// Gets the binding metadata part of this input binding data instance.
        /// </summary>
        public abstract BindingMetadata BindingMetadata { get; }
        
        /// <summary>
        /// Gets or sets the value of the binding result.
        /// </summary>
        public abstract T? Value { get; set; }
    }
}
