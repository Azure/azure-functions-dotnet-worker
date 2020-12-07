using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class Function5
    {
        private readonly IHttpResponderService _responderService;

        public Function5(IHttpResponderService responderService)
        {
            _responderService = responderService;
        }

        [FunctionName(nameof(Function5))]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionExecutionContext executionContext)
        {
            var logger = executionContext.Logger;
            logger.LogInformation("message logged");

            return _responderService.ProcessRequest(req);
        }
    }

    public interface IHttpResponderService
    {
        HttpResponseData ProcessRequest(HttpRequestData httpRequest);
    }

    public class DefaultHttpResponderService : IHttpResponderService
    {
        public DefaultHttpResponderService()
        {

        }

        public HttpResponseData ProcessRequest(HttpRequestData httpRequest)
        {
            var response = new HttpResponseData(HttpStatusCode.OK);
            var headers = new Dictionary<string, string>();
            headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
            headers.Add("Content", "Content - Type: text / html; charset = utf - 8");

            response.Headers = headers;
            response.Body = "Welcome to .NET 5!!";

            return response;
        }
    }
}
