// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Context
{
    internal class GrpcFunctionInvocation : FunctionInvocation
    {
        private InvocationRequest _invocationRequest;

        public GrpcFunctionInvocation(InvocationRequest invocationRequest)
        {
            _invocationRequest = invocationRequest;
            TraceContext = new DefaultTraceContext(_invocationRequest.TraceContext.TraceParent, _invocationRequest.TraceContext.TraceState);
            ValueProvider = new GrpcValueProvider(invocationRequest.InputData, invocationRequest.TriggerMetadata);
        }

        public override IValueProvider ValueProvider { get; set; }

        public override string Id => _invocationRequest.InvocationId;

        public override string FunctionId => _invocationRequest.FunctionId;

        public override TraceContext TraceContext { get; }
    }
}
