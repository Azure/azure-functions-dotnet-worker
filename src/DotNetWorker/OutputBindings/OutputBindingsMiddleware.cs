// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal sealed class OutputBindingsMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            await next(context);

            AddOutputBindings(context);
        }

        internal static void AddOutputBindings(FunctionContext context)
        {
            object? result = context.InvocationResult;

            if (result != null)
            {
                var functionBindings = context.Features.Get<IFunctionBindingsFeature>();
                functionBindings?.OutputBindings.BindOutputInContext(context);
            }
        }
    }
}
