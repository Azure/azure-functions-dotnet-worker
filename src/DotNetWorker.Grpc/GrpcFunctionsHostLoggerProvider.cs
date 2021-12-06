// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Azure.Core.Serialization;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class GrpcFunctionsHostLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ChannelWriter<StreamingMessage> _channelWriter;
        private readonly ObjectSerializer _serializer;
        private IExternalScopeProvider? _scopeProvider;

        public GrpcFunctionsHostLoggerProvider(GrpcHostChannel outputChannel, IOptions<WorkerOptions> workerOptions)
        {
            _channelWriter = outputChannel.Channel.Writer;

            if (workerOptions?.Value?.Serializer != null)
            {
                _serializer = workerOptions.Value.Serializer;
            }
            else
            {
                _serializer = new JsonObjectSerializer();
            }

        }

        public ILogger CreateLogger(string categoryName) => new GrpcFunctionsHostLogger(categoryName, _channelWriter, _scopeProvider!, _serializer);

        public void Dispose()
        {
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
