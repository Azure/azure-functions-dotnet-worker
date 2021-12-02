// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Azure.Core.Serialization;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class GrpcFunctionsHostLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ChannelWriter<StreamingMessage> _channelWriter;
        private IExternalScopeProvider? _scopeProvider;
        private ObjectSerializer? _serializer;

        public GrpcFunctionsHostLoggerProvider(GrpcHostChannel outputChannel)
        {
            _channelWriter = outputChannel.Channel.Writer;
        }

        public ILogger CreateLogger(string categoryName) => new GrpcFunctionsHostLogger(categoryName, _channelWriter, _scopeProvider!, _serializer!);

        public void Dispose()
        {
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void SetObjectSerializer(ObjectSerializer serializer)
        {
            _serializer = serializer;
        }
    }
}
