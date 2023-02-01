using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public class HttpProxyTrigger
    {
        [Function(nameof(HttpProxyTrigger))]
        public HttpResponse Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, 
            FunctionContext functionContext, 
            CancellationToken cancellationToken)
        {
            var response = req.HttpContext.Response;

            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteAsync("Welcome to Azure Functions!");

            return response;
        }
    }
}
