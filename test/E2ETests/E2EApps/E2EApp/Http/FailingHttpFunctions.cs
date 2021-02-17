using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class FailingHttpFunctions
    {
        [FunctionName(nameof(ExceptionFunction))]
        public static HttpResponseData ExceptionFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            context.Logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            throw new Exception("This should never succeed!");
        }
    }
}
