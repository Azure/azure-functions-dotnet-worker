using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class HttpTriggerWeb
    {
        private readonly ILogger<HttpTriggerWeb> _logger;

        public HttpTriggerWeb(ILogger<HttpTriggerWeb> logger)
        {
            _logger = logger;
        }

        [Function("HttpTriggerWeb")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
