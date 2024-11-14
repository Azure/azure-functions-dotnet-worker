using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AspNetIntegration
{
    //<docsnippet_aspnetcore_multiple_outputs>
    public class MultipleOutputBindings
    {
        private readonly ILogger<MultipleOutputBindings> _logger;
        public MultipleOutputBindings(ILogger<MultipleOutputBindings> logger)
        {
            _logger = logger;
        }
        [Function("MultipleOutputBindings")]
        public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var myObject = new MyOutputType { Result = new OkObjectResult("C# HTTP trigger function processed a request."), MessageText = "some output"};
            return myObject;
        }
        public class MyOutputType
        {
            [HttpResult]
            public IActionResult? Result { get; set; }

            [QueueOutput("myQueue")]
            public string? MessageText { get; set; }
        }
    }
    //</docsnippet_aspnetcore_multiple_outputs>

    public class MultipleOutputBindingsHttpResponseData
    {
        private readonly ILogger<MultipleOutputBindingsHttpResponseData> _logger;
        public MultipleOutputBindingsHttpResponseData(ILogger<MultipleOutputBindingsHttpResponseData> logger)
        {
            _logger = logger;
        }

        [Function("MultipleOutputBindingsHttpResponseData")]
        public MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse();
            response.WriteStringAsync("C# HTTP trigger function processed a request.");

            var myObject = new MyOutputType { Result = response, MessageText = "some output" };
            return myObject;
        }
        public class MyOutputType
        {
            [HttpResult]
            public HttpResponseData? Result { get; set; }

            [QueueOutput("myQueue")]
            public string? MessageText { get; set; }
        }
    }
}

