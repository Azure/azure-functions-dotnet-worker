// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CustomMiddleware
{
    public static class HttpTriggerWithMultipleOutputBindings
    {
        [Function(nameof(HttpTriggerWithMultipleOutputBindings))]
        public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            if (req.Url.Query.Contains("throw-exception"))
            {
                throw new Exception("App code failed");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("Success!");

            return new MyOutputType()
            {
                Name = "Azure Functions",
                HttpResponse = response
            };
        }
    }

    public class MyOutputType
    {
        [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
        public string Name { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
