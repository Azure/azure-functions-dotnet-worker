// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    internal partial class FunctionsApplication
    {
        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception?> _functionDefinitionCreated =
                WorkerMessage.Define<string, string>(LogLevel.Trace, new EventId(1, nameof(FunctionDefinitionCreated)),
                    "Function definition for '{functionName}' created with id '{functionid}'.");

            private static readonly Action<ILogger, string, string, Exception?> _invocationError =
                LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(2, nameof(InvocationError)),
                    "Function '{functionName}', Invocation id '{invocationId}': An exception was thrown by the invocation.");

            public static void FunctionDefinitionCreated(ILogger<FunctionsApplication> logger, FunctionDefinition functionDefinition)
            {
                _functionDefinitionCreated(logger, functionDefinition.Name, functionDefinition.Id, null);
            }

            public static void InvocationError(ILogger<FunctionsApplication> logger, string functionName, string invocationId, Exception exception)
            {
                _invocationError(logger, functionName, invocationId, exception);
            }
        }
    }
}
