// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class FunctionExecutionMiddleware
    {
        private readonly IFunctionExecutor _functionExecutor;

        public FunctionExecutionMiddleware(IFunctionExecutor functionExecutor)
        {
            _functionExecutor = functionExecutor;
        }

        public Task Invoke(FunctionContext context)
        {
            return _functionExecutor.ExecuteAsync(context);
        }
    }
}
