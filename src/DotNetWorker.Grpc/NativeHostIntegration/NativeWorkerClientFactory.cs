// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    internal class NativeWorkerClientFactory : IWorkerClientFactory
    {
        private readonly GrpcHostChannel _hostChannel;

        public NativeWorkerClientFactory(GrpcHostChannel hostChannel)
        {
            _hostChannel = hostChannel;
        }

        public Task<IWorkerClient> StartClientAsync(IMessageProcessor messageProcessor, CancellationToken token)
        {
            var nativeHostData = NativeMethods.GetNativeHostData();

            var client = new NativeWorkerClient(messageProcessor, _hostChannel, nativeHostData);
            client.Start();

            return Task.FromResult<IWorkerClient>(client);
        }
    }
}
