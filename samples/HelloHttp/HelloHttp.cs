using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HelloHttp
{
    public class HelloHttp
    {
        private readonly ILogger<HelloHttp> _logger;

        public HelloHttp(ILogger<HelloHttp> logger)
        {
            _logger = logger;
        }

        [Function("hellohttp")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.WriteString("Hello, World");
            return response;
        }
    }
}
