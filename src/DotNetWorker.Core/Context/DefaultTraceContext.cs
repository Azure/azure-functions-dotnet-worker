// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    internal sealed class DefaultTraceContext : TraceContext
    {
        public DefaultTraceContext(string traceParent, string traceState, IReadOnlyDictionary<string, string> attributes)
        {
            TraceParent = traceParent;
            TraceState = traceState;
            Attributes = attributes;
        }

        public override string TraceParent { get; }

        public override string TraceState { get; }

        public override IReadOnlyDictionary<string, string> Attributes { get; }
    }
}
