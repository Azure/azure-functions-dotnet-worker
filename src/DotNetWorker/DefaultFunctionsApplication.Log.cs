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

            public static void FunctionDefinitionCreated(ILogger<FunctionsApplication> logger, FunctionDefinition functionDefinition)
            {
                _functionDefinitionCreated(logger, functionDefinition.Name, functionDefinition.Id, null);
            }
        }
    }
}
