// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace CustomMiddleware
{
    public class MyCustomMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // This is added pre-function execution, function will have access to this information
            // in the context.Items dictionary
            context.Items.Add("middlewareitem", "Hello, from middleware");
            
            await next(context);

            // This happens after function execution. We can inspect the context after the function
            // was invoked
            if (context.Items.TryGetValue("functionitem", out object value) && value is string message)
            {
                ILogger logger = context.GetLogger<MyCustomMiddleware>();

                logger.LogInformation("From function: {message}", message);
            }
        }
    }
}
