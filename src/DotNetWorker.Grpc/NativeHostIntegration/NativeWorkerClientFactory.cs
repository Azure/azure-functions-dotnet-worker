// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    internal class NativeWorkerClientFactory : IWorkerClientFactory
    {
        private readonly GrpcHostChannel _hostChannel;

        public NativeWorkerClientFactory(GrpcHostChannel hostChannel)
        {
            _hostChannel = hostChannel;
        }

        public IWorkerClient CreateClient(IMessageProcessor messageProcessor)
        {
            return new NativeWorkerClient(messageProcessor, _hostChannel);
        }
    }
}
