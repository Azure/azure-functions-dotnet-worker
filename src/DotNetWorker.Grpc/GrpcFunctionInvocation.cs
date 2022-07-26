// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal sealed class GrpcFunctionInvocation : FunctionInvocation, IExecutionRetryFeature
    {
        private readonly InvocationRequest _invocationRequest;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private RetryContext? _retryContext;

        public GrpcFunctionInvocation(InvocationRequest invocationRequest, CancellationTokenSource cancellationTokenSource)
        {
            _invocationRequest = invocationRequest;
            _cancellationTokenSource = cancellationTokenSource;
            TraceContext = new DefaultTraceContext(_invocationRequest.TraceContext.TraceParent, _invocationRequest.TraceContext.TraceState);
        }

        public override string Id => _invocationRequest.InvocationId;

        public override string FunctionId => _invocationRequest.FunctionId;

        public override TraceContext TraceContext { get; }

        public RetryContext Context => _retryContext ??= new GrpcRetryContext(_invocationRequest.RetryContext);

        public override CancellationToken CancellationToken => _cancellationTokenSource.Token;
    }
}
