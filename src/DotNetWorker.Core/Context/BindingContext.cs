// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Exposes binding infomation for a given function context.
    /// </summary>
    public abstract class BindingContext
    {
        /// <summary>
        /// Gets the binding data information for the current context.
        /// This contains all of the trigger defined metadata.
        /// </summary>
        public abstract IReadOnlyDictionary<string, object?> BindingData { get; }
    }
}
