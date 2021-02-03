// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.WebJobs.Script.Grpc.Messages.RpcLog.Types;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    public class InvocationLogger : ILogger
    {
        private string _invocationId;
        private ChannelWriter<StreamingMessage> _channelWriter;

        public InvocationLogger(string invocationId, ChannelWriter<StreamingMessage> channelWriter)
        {
            _invocationId = invocationId;
            _channelWriter = channelWriter;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var response = new StreamingMessage();
            string message = formatter(state, exception);
            response.RpcLog = new RpcLog
            {
                EventId = eventId.ToString(),
                Exception = exception.ToRpcException(),
                LogCategory = RpcLogCategory.User,
                Level = ToRpcLogLevel(logLevel),
                InvocationId = _invocationId,
                Message = message
            };
            _channelWriter.TryWrite(response);
        }

        private static Level ToRpcLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return Level.Trace;
                case LogLevel.Debug:
                    return Level.Debug;
                case LogLevel.Information:
                    return Level.Information;
                case LogLevel.Warning:
                    return Level.Warning;
                case LogLevel.Error:
                    return Level.Error;
                case LogLevel.Critical:
                    return Level.Critical;
                case LogLevel.None:
                default:
                    return Level.None;
            }
        }
    }
}
