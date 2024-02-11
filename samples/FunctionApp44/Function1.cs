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
            var buildConfiguration = string.Empty;
#if DEBUG
            buildConfiguration = "Debug";
#else
        buildConfiguration = "Release";
#endif

            _logger.LogInformation($"C# HTTP trigger function processed a request.buildConfiguration:{buildConfiguration}");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.WriteString($"Hello Http!. Published on 2024 02 11. buildConfiguration:{buildConfiguration}");
            return response;
        }
    }
}
