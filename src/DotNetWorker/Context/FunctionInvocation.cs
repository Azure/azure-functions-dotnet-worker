// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Context
{
    public abstract class FunctionInvocation
    {
        public abstract IValueProvider ValueProvider { get; set; }

        public abstract string InvocationId { get; set; }

        public abstract string FunctionId { get; set; }

        public abstract string TraceParent { get; set; }

        public abstract string TraceState { get; set; }
    }
}
