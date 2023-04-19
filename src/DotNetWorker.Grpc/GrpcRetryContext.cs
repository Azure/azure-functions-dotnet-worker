// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RetryContextMessage = Microsoft.Azure.Functions.Worker.Grpc.Messages.RetryContext;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal sealed class GrpcRetryContext : RetryContext
    {
        private readonly RetryContextMessage _retryContext;

        public GrpcRetryContext(RetryContextMessage retryContext)
        {
            _retryContext = retryContext;
        }

        public override int RetryCount => _retryContext.RetryCount;

        public override int MaxRetryCount => _retryContext.MaxRetryCount;
    }
}
