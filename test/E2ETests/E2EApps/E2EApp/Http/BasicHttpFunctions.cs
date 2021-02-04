using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class BasicHttpFunctions
    {
        [FunctionName(nameof(HelloFromQuery))]
        public static HttpResponseData HelloFromQuery(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionExecutionContext context)
        {
            context.Logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            var queryName = HttpUtility.ParseQueryString(req.Url.Query)["name"];
            
            if (!string.IsNullOrEmpty(queryName))
            {
                var response = new HttpResponseData(HttpStatusCode.OK);
                response.Body = "Hello " + queryName;
                return response;
            }
            else
            {
                return new HttpResponseData(HttpStatusCode.BadRequest);
            }
        }

        [FunctionName(nameof(HelloFromJsonBody))]
        public static HttpResponseData HelloFromJsonBody(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionExecutionContext context)
        {
            context.Logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            var body = Encoding.UTF8.GetString(req.Body.Value.Span);

            if (!string.IsNullOrEmpty(body))
            {
                var serliazedBody = (CallerName)JsonSerializer.Deserialize(body, typeof(CallerName));
                var response = new HttpResponseData(HttpStatusCode.OK);
                response.Body = "Hello " + serliazedBody.Name;
                return response;
            }
            else
            {
                return new HttpResponseData(HttpStatusCode.BadRequest);
            }
        }
        
        public class CallerName
        {
            public string Name { get; set; }
        }
    }
}
