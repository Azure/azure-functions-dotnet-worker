// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.RpcLog.Types;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// A logger that sends logs back to the Functions host.
    /// </summary>
    internal class GrpcFunctionsHostLogWriter : ISystemLogWriter, IUserLogWriter, IUserMetricWriter
    {
        private readonly ChannelWriter<StreamingMessage> _channelWriter;
        private readonly ObjectSerializer _serializer;

        public GrpcFunctionsHostLogWriter(GrpcHostChannel channel, IOptions<WorkerOptions> workerOptions)
        {
            _channelWriter = channel?.Channel?.Writer ?? throw new ArgumentNullException(nameof(channel));
            _serializer = workerOptions.Value.Serializer ?? throw new ArgumentNullException(nameof(workerOptions.Value.Serializer), "Serializer on WorkerOptions cannot be null");
        }

        public void WriteUserLog<TState>(IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Log(RpcLogCategory.User, scopeProvider, categoryName, logLevel, eventId, state, exception, formatter);
        }

        public void WriteUserMetric(IExternalScopeProvider scopeProvider, IDictionary<string, object> properties)
        {
            var response = new StreamingMessage();
            var rpcMetric = new RpcLog
            {
                LogCategory = RpcLogCategory.CustomMetric,
            };

            foreach (var kvp in properties)
            {
                rpcMetric.PropertiesMap.Add(kvp.Key, kvp.Value.ToRpc(_serializer));
            }

            // Grab the invocation id from the current scope, if present.
            rpcMetric = AppendInvocationIdToLog(rpcMetric, scopeProvider);

            response.RpcLog = rpcMetric;

            _channelWriter.TryWrite(response);
        }

        public void WriteSystemLog<TState>(IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Log(RpcLogCategory.System, scopeProvider, categoryName, logLevel, eventId, state, exception, formatter);
        }

        public void Log<TState>(RpcLogCategory category, IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var response = new StreamingMessage();
            var rpcLog = new RpcLog
            {
                EventId = eventId.ToString(),
                Exception = exception.ToRpcException(),
                Category = categoryName,
                LogCategory = category,
                Level = ToRpcLogLevel(logLevel),
                Message = formatter(state, exception)
            };

            // Grab the invocation id from the current scope, if present.
            rpcLog = AppendInvocationIdToLog(rpcLog, scopeProvider);

            response.RpcLog = rpcLog;

            _channelWriter.TryWrite(response);
        }

        private RpcLog AppendInvocationIdToLog(RpcLog rpcLog, IExternalScopeProvider scopeProvider)
        {
            scopeProvider.ForEachScope((scope, log) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (var pair in properties)
                    {
                        if (pair.Key == TraceConstants.InternalKeys.FunctionInvocationId)
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
