// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    /// <summary>
    /// Propagates baggage items within the current context.
    /// </summary>
    public interface IBaggagePropagator
    {
        /// <summary>
        /// Sets the baggage for the current operation, allowing additional key-value pairs to be associated with it.
        /// </summary>
        /// <remarks>This method is typically used to attach contextual information to the operation,
        /// which can be useful for logging or tracing purposes. Ensure that the baggage does not exceed any size limits
        /// imposed by the underlying system.</remarks>
        /// <param name="baggage">An enumerable collection of key-value pairs representing the baggage to be set. Each key must be unique and
        /// cannot be null.</param>
        /// <returns>An IDisposable that, when disposed, will clear the baggage set for the current operation.</returns>
        public IDisposable? SetBaggage(IEnumerable<KeyValuePair<string, string>> baggage);
    }
}
