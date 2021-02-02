// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class DefaultFunctionExecutor : IFunctionExecutor
    {
        public async Task ExecuteAsync(FunctionExecutionContext context)
        {
            var invoker = context.FunctionDefinition.Invoker;
            object instance = invoker.CreateInstance(context.InstanceServices);
            object result = await invoker.InvokeAsync(instance, context.FunctionDefinition.Parameters.Select(p => p.Value).ToArray());
            context.InvocationResult = result;
        }
    }
}
