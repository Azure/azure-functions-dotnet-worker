using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class FailingHttpFunctions
    {
        [FunctionName(nameof(ExceptionFunction))]
        public static HttpResponseData ExceptionFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionExecutionContext context)
        {
            context.Logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            throw new Exception("This should never succeed!");
        }
    }
}
