// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    internal sealed class DefaultTraceContext : TraceContext
    {
        public DefaultTraceContext(string traceParent, string traceState)
        {
            TraceParent = traceParent;
            TraceState = traceState;
        }

        public override string TraceParent { get; }

        public override string TraceState { get; }
    }
}
