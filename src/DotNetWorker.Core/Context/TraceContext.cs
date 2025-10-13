// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The trace context for the current invocation.
    /// </summary>
    public abstract class TraceContext
    {
        /// <summary>
        /// Gets the identity of the incoming invocation in a tracing system.
        /// </summary>
        public abstract string TraceParent { get; }

        /// <summary>
        /// Gets the state data.
        /// </summary>
        public abstract string TraceState { get; }

        /// <summary>
        /// Gets the attributes associated with the trace.
        /// </summary>
        public abstract IReadOnlyDictionary<string, string> Attributes { get; }
    }
}
