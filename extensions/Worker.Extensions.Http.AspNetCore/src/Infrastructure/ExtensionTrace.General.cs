// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Infrastructure
{
    internal sealed partial class ExtensionTrace : ILogger
    {
        public void HttpContextSet(string invocationId, string requestId)
        {
            GeneralLog.HttpContextSet(_defaultLogger, invocationId, requestId);
        }

        public void FunctionContextSet(string invocationId)
        {
            GeneralLog.FunctionContextSet(_defaultLogger, invocationId);
        }

        public void NoHttpResponseReturned(string functionName, string invocationId)
        {
            GeneralLog.NoHttpResponseReturned(_defaultLogger, functionName, invocationId);
        }

        private static partial class GeneralLog
        {
            [LoggerMessage(1, LogLevel.Debug, @"HttpContext set for invocation ""{InvocationId}"", Request id ""{RequestId}"".", EventName = nameof(HttpContextSet))]
            public static partial void HttpContextSet(ILogger logger, string invocationId, string requestId);

            [LoggerMessage(2, LogLevel.Debug, @"FunctionContext set for invocation ""{InvocationId}"".", EventName = nameof(FunctionContextSet))]
            public static partial void FunctionContextSet(ILogger logger, string invocationId);

            [LoggerMessage(3, LogLevel.Trace, @"No HTTP response returned from function '{FunctionName}', invocation {InvocationId}.", EventName = nameof(NoHttpResponseReturned))]
            public static partial void NoHttpResponseReturned(ILogger logger, string functionName, string invocationId);
        }
    }
}
