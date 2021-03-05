// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class BasicHttpFunctions
    {
        [Function(nameof(HelloFromQuery))]
        public static HttpResponseData HelloFromQuery(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(HelloFromQuery));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            var queryName = HttpUtility.ParseQueryString(req.Url.Query)["name"];

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

        public class MyResponse
        {
            public string Name { get; set; }
        }

    }
}
