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
        private readonly HttpClient _httpClient;

        public HttpTrigger(ILogger<HttpTrigger> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [Function("HttpTrigger")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation("Current Activity - " + Activity.Current?.Id);

            _httpClient.GetAsync("https://www.bing.com").GetAwaiter().GetResult();
            return new OkObjectResult("Welcome to Azure Functions! " + DateTime.Now);
        }
    }
}
