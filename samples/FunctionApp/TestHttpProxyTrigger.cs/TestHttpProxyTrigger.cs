using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public class TestHttpProxyTrigger
    {
        [Function("TestHttpProxyTrigger")]
        public HttpResponse Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, 
            FunctionContext functionContext, 
            CancellationToken cancellationToken)
        {
            var response = req.HttpContext.Response;

            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteAsync("Welcome to Azure Functions!");

            //var httpResponseData = n

            return response;
        }
    }
}
