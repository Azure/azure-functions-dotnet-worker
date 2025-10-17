// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class FunctionExecutionMiddleware(IFunctionExecutor functionExecutor)
    {
        private readonly IFunctionExecutor _functionExecutor = functionExecutor
            ?? throw new ArgumentNullException(nameof(functionExecutor));

        public Task Invoke(FunctionContext context)
        {
            if (context.Features.Get<IFunctionExecutor>() is { } executor)
            {
                return executor.ExecuteAsync(context).AsTask();
            }

            return _functionExecutor.ExecuteAsync(context).AsTask();
        }
    }
}
