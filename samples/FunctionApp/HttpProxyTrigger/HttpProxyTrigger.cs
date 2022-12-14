using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public class HttpProxyTrigger
    {
        [Function(nameof(HttpProxyTrigger))]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, 
            FunctionContext functionContext, 
            CancellationToken cancellationToken)
        {
            return new OkObjectResult("Welcome To Azure Functions!");
        }
    }
}
