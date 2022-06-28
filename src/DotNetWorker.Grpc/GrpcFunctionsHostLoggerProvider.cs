// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    internal class GrpcFunctionsHostLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ChannelWriter<StreamingMessage> _channelWriter;
        private readonly WorkerOptions _workerOptions;
        private readonly ObjectSerializer _serializer;
        private IExternalScopeProvider? _scopeProvider;

        public GrpcFunctionsHostLoggerProvider(GrpcHostChannel outputChannel, IOptions<WorkerOptions> workerOptions)
        {
            _channelWriter = outputChannel.Channel.Writer;
            _workerOptions = workerOptions.Value;
            _serializer = _workerOptions.Serializer ?? throw new ArgumentNullException(nameof(workerOptions.Value.Serializer), "Serializer on WorkerOptions cannot be null");
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_workerOptions.DisableHostLogger)
            {
                return NullLogger.Instance;
            }

            return new GrpcFunctionsHostLogger(categoryName, _channelWriter, _scopeProvider!, _serializer);
        }

        public void Dispose()
        {
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
