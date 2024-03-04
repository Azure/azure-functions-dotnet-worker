using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AspNetIntegration
{
    public class MultipleOutputBindings
    {
        private readonly ILogger<MultipleOutputBindings> _logger;

        public MultipleOutputBindings(ILogger<MultipleOutputBindings> logger)
        {
            _logger = logger;
        }

        [Function("MultipleOutputBindings")]
        public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var myObject = new MyOutputType
            {
                HttpResponse = new OkObjectResult("C# HTTP trigger function processed a request."),
                Name = "some name"
            };

            return myObject;
        }

        public class MyOutputType
        {
            [HttpResponseOutput()]
            public IActionResult HttpResponse { get; set; }

            [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
            public string Name { get; set; }
        }
    }
}
