// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    /// <summary>
    /// Propagates baggage items within the current context.
    /// </summary>
    public interface IBaggagePropagator
    {
        /// <summary>
        /// Sets the baggage items for the current context, allowing for additional metadata to be associated with the
        /// operation.
        /// </summary>
        /// <remarks>This method can be used to attach contextual information that may be useful for
        /// tracing or logging purposes. Ensure that the baggage items do not exceed any size limits imposed by the
        /// underlying system.</remarks>
        /// <param name="baggage">The collection of key-value pairs representing the baggage items to be set. Each key must be a non-empty
        /// string, and the corresponding value can be null or an empty string.</param>
        public void SetBaggage(IEnumerable<KeyValuePair<string, string>> baggage);

        /// <summary>
        /// Clears the specified baggage items from the current context.
        /// </summary>
        /// <param name="baggage">The collection of key-value pairs representing the baggage items to be cleared.</param>
        void ClearBaggage(IEnumerable<KeyValuePair<string, string>> baggage);
    }
}
