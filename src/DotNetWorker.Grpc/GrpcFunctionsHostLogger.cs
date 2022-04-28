// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Logging.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.RpcLog.Types;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// A logger that sends logs back to the Functions host.
    /// </summary>
    internal class GrpcFunctionsHostLogger : ILogger
    {
        private readonly string _category;
        private readonly ChannelWriter<StreamingMessage> _channelWriter;
        private readonly IExternalScopeProvider _scopeProvider;
        private readonly ObjectSerializer _serializer;

        public GrpcFunctionsHostLogger(string category, ChannelWriter<StreamingMessage> channelWriter, IExternalScopeProvider scopeProvider, ObjectSerializer serializer)
        {
            _category = category ?? throw new ArgumentNullException(nameof(category));
            _channelWriter = channelWriter ?? throw new ArgumentNullException(nameof(channelWriter));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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
            if (eventId.Name == LogConstants.MetricEventId.Name)
            {
                LogMetric((IDictionary<string, object>)state!);
            }
            else
            {
                var response = new StreamingMessage();
                string message = formatter(state, exception);
                var rpcLog = new RpcLog
                {
                    EventId = eventId.ToString(),
                    Exception = exception.ToRpcException(),
                    Category = _category,
                    LogCategory = WorkerMessage.IsSystemLog ? RpcLogCategory.System : RpcLogCategory.User,
                    Level = ToRpcLogLevel(logLevel),
                    Message = message
                };

                // Grab the invocation id from the current scope, if present.
                rpcLog = AppendInvocationIdToLog(rpcLog);

                response.RpcLog = rpcLog;

                _channelWriter.TryWrite(response);
            }
        }

        private void LogMetric(IDictionary<string, object> state)
        {
            if (state == null)
            {
                return;
            }

            var response = new StreamingMessage();
            var rpcMetric = new RpcLog
            {
                LogCategory = RpcLogCategory.CustomMetric,
            };

            foreach (var kvp in state)
            {
                rpcMetric.PropertiesMap.Add(kvp.Key, kvp.Value.ToRpc(_serializer));
            }

            // Grab the invocation id from the current scope, if present.
            rpcMetric = AppendInvocationIdToLog(rpcMetric);

            response.RpcLog = rpcMetric;

            _channelWriter.TryWrite(response);
        }

        private RpcLog AppendInvocationIdToLog(this RpcLog rpcLog)
        {
            _scopeProvider?.ForEachScope((scope, log) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (var pair in properties)
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

            return rpcLog;
        }

        private static Level ToRpcLogLevel(LogLevel logLevel) =>
            logLevel switch
            {
                LogLevel.Trace => Level.Trace,
                LogLevel.Debug => Level.Debug,
                LogLevel.Information => Level.Information,
                LogLevel.Warning => Level.Warning,
                LogLevel.Error => Level.Error,
                LogLevel.Critical => Level.Critical,
                _ => Level.None,
            };

        private class EmptyDisposable : IDisposable
        {
            public static IDisposable Instance = new EmptyDisposable();

            public void Dispose()
            {
            }
        }
    }
}
