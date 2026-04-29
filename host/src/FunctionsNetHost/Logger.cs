// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using FunctionsNetHost.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost
{
    internal static class Logger
    {
        private const string RpcLogMessagePrefix = "FunctionsNetHost:";

        /// <summary>
        /// Logs a trace message if trace level logging is enabled.
        /// </summary>
        internal static void LogTrace(string message)
        {
            if (Configuration.IsTraceLogEnabled)
            {
                Log(message);
            }
        }

        internal static void Log(string message)
        {
            var logMessage = new StreamingMessage
            {
                RpcLog = new RpcLog
                {
                    Category = "FunctionsNetHost",
                    Level = RpcLog.Types.Level.Information,
                    LogCategory = RpcLog.Types.RpcLogCategory.System,
                    Message = $"{RpcLogMessagePrefix} {message}"
                }
            };

            MessageChannel.Instance.OutboundChannel.Writer.TryWrite(logMessage);
        }
    }
}
