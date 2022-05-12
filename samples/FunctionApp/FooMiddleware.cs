using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace FunctionApp
{
    internal class FooMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            BindingMetadata blobBindingMetaData = context.FunctionDefinition
                                             .InputBindings.Values
                                             .Where(a => a.Type.Contains("queue", StringComparison.InvariantCultureIgnoreCase))
                                             .FirstOrDefault();

            if (blobBindingMetaData != null)
            {
                var bindingResult = await context.BindInputAsync<Book>(blobBindingMetaData);
                // Update a property value. 
                // This will be reflected in the function parameter value.
                bindingResult.Value.name = "Edited name in middleware";

                // or you could even replace the entire object
               bindingResult.Value = new Book { name = "Totally different object" };
            }


            await next(context);

        }
    }
}
