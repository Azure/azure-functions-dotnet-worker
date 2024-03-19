using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class HttpTrigger
    {
        private readonly ILogger<HttpTrigger> _logger;

        public HttpTrigger(ILogger<HttpTrigger> logger)
        {
            _logger = logger;
        }

        [Function("HttpTrigger")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation("1");
            _logger.LogInformation("Current Activity - " + Activity.Current?.Id);
            HttpClient client = new HttpClient();
            client.GetAsync("https://www.bing.com").GetAwaiter().GetResult();
            return new OkObjectResult("Welcome to Azure Functions! " + DateTime.Now);
        }
    }
}
