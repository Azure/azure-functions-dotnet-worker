// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.WebJobs.Script.Grpc.Messages.RpcLog.Types;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// A logger that sends logs back to the Functions host.
    /// </summary>
    internal class GrpcFunctionsHostLogger : ILogger
    {
        private readonly ChannelWriter<StreamingMessage> _channelWriter;
        private IExternalScopeProvider _scopeProvider;

        public GrpcFunctionsHostLogger(ChannelWriter<StreamingMessage> channelWriter, IExternalScopeProvider scopeProvider)
        {
            _channelWriter = channelWriter ?? throw new ArgumentNullException(nameof(channelWriter));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            // The built-in DI wire-up guarantees that scope provider will be set.
            return _scopeProvider!.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var response = new StreamingMessage();
            string message = formatter(state, exception);
            var rpcLog = new RpcLog
            {
                EventId = eventId.ToString(),
                Exception = exception.ToRpcException(),
                LogCategory = WorkerLogger.IsSystemLog ? RpcLogCategory.System : RpcLogCategory.User,
                Level = ToRpcLogLevel(logLevel),
                Message = message
            };

            // Grab the invocation id from the current scope, if present.
            _scopeProvider.ForEachScope((scope, log) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (KeyValuePair<string, object> pair in properties)
                    {
                        if (pair.Key == FunctionInvocationScope.FunctionInvocationIdKey)
                        {
                            log.InvocationId = pair.Value?.ToString();
                            break;
                        }
                    }
                }
            },
            rpcLog);

            response.RpcLog = rpcLog;

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

        private class EmptyDisposable : IDisposable
        {
            public static IDisposable Instance = new EmptyDisposable();

            public void Dispose()
            {
            }
        }
    }
}
