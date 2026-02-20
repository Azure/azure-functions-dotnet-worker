// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class BasicHttpFunctions
    {
        [Function("HelloPascal")]
        public static HttpResponseData Hello(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Hello));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("Hello!");
            return response;
        }

        [Function("HelloAllCaps")]
        public static HttpResponseData HELLO(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(HELLO));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("HELLO!");
            return response;
        }

        [Function(nameof(HelloFromQuery))]
        public static HttpResponseData HelloFromQuery(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(HelloFromQuery));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");

            var queryName = req.Query["name"];

            if (!string.IsNullOrEmpty(queryName))
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteString("Hello " + queryName);
                return response;
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [Function(nameof(HelloFromJsonBody))]
        public static HttpResponseData HelloFromJsonBody(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(HelloFromJsonBody));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            var body = req.ReadAsString();

            if (!string.IsNullOrEmpty(body))
            {
                var serliazedBody = (CallerName)JsonSerializer.Deserialize(body, typeof(CallerName));
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteString("Hello " + serliazedBody.Name);
                return response;
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        public class CallerName
        {
            public string Name { get; set; }
        }

        [Function(nameof(HelloUsingPoco))]
        public static MyResponse HelloUsingPoco(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(HelloUsingPoco));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");

            return new MyResponse { Name = "Test" };
        }

        [Function(nameof(HelloWithNoResponse))]
        public static Task HelloWithNoResponse(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(HelloWithNoResponse));
            logger.LogInformation($".NET Worker HTTP trigger function processed a request");

            return Task.CompletedTask;
        }

        [Function(nameof(PocoFromBody))]
        public static HttpResponseData PocoFromBody(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            [FromBody] CallerName caller,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(PocoFromBody));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Greetings {caller.Name}");
            return response;
        }

        [Function(nameof(PocoBeforeRouteParameters))]
        public static Task PocoBeforeRouteParameters(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "{region}/{category}/" + nameof(PocoBeforeRouteParameters))] [FromBody] CallerName caller,
            string region,
            string category,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(PocoBeforeRouteParameters));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            return Task.CompletedTask;
        }

        [Function(nameof(PocoAfterRouteParameters))]
        public static HttpResponseData PocoAfterRouteParameters(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "{region}/{category}/" + nameof(PocoAfterRouteParameters))] HttpRequestData req,
            string region,
            string category,
            [FromBody] CallerName caller,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(PocoAfterRouteParameters));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"{region} {category} {caller.Name}");
            return response;
        }

        [Function(nameof(IntFromRoute))]
        public static HttpResponseData IntFromRoute(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = nameof(IntFromRoute) + "/{value}")] HttpRequestData req,
            int value,
            FunctionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(value.ToString(CultureInfo.InvariantCulture));
            return response;
        }

        [Function(nameof(DoubleFromRoute))]
        public static HttpResponseData DoubleFromRoute(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = nameof(DoubleFromRoute) + "/{value}")] HttpRequestData req,
            double value,
            FunctionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(value.ToString(CultureInfo.InvariantCulture));
            return response;
        }

        public class MyResponse
        {
            public string Name { get; set; }
        }

    }
}
