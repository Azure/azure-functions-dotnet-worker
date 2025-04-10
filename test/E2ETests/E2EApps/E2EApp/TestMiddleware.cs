using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public class MyCustomMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                // This is added pre-function execution, function will have access to this information
                // in the context.Items dictionary
                context.Items.Add("middlewareitem", "Hello, from middleware");

                await next(context);

                // This happens after function execution. We can inspect the context after the function
                // was invoked
                if (context.Items.TryGetValue("functionitem", out var value) && value is string message)
                {
                    ILogger logger = context.GetLogger<MyCustomMiddleware>();

                    logger.LogInformation("From function: {message}", message);
                }
            }
            catch (Exception ex)
            {
                // This is where we can handle exceptions thrown by the function
                ILogger logger = context.GetLogger<MyCustomMiddleware>();
                logger.LogError(ex, "An error occurred in the middleware");

                // Optionally rethrow the exception to propagate it further
                throw;
            }
        }
    }
}
