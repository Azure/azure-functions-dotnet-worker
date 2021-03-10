// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal partial class DefaultFunctionExecutor
    {
        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception?> _modelBindingFeatureUnavailable =
                WorkerMessage.Define<string, string>(LogLevel.Warning, new EventId(2, nameof(ModelBindingFeatureUnavailable)),
                    "The feature " + nameof(IModelBindingFeature) + " was not available for invocation '{invocationId}' of function '{functionName}'. Unable to process input bindings.");

            public static void ModelBindingFeatureUnavailable(ILogger<DefaultFunctionExecutor> logger, FunctionContext context)
            {
                _modelBindingFeatureUnavailable(logger, context.InvocationId, context.FunctionId, null);
            }
        }
    }
}
