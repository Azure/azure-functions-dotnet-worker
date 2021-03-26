// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FunctionApp
{
    public static class Function3
    {
        [Function("Function3")]
        public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("Success!");

            return new MyOutputType()
            {
                Name = "some name",
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
