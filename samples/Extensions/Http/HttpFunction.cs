// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class HttpFunction
    {
        //<docsnippet_http_trigger>
        [Function("HttpFunction")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            //<docsnippet_logging>
            var logger = executionContext.GetLogger("HttpFunction");
            logger.LogInformation("message logged");
            //</docsnippet_logging>

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            
            response.WriteString("Welcome to .NET 5!!");

            return response;
        }
        //</docsnippet_http_trigger>
    }
}
