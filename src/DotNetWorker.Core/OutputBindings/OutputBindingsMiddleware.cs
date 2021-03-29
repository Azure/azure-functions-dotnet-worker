// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal sealed class OutputBindingsMiddleware
    {
        public static async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            await next(context);

            AddOutputBindings(context);
        }

        internal static void AddOutputBindings(FunctionContext context)
        {
            var functionBindings = context.GetBindings();
            object? result = functionBindings.InvocationResult;

            if (result != null)
            {
                functionBindings.OutputBindingsInfo.BindOutputInContext(context);
            }
        }
    }
}
