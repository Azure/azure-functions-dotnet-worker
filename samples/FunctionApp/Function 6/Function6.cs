// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public static class Function6
    {
        [Function("Function6")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var headers = req.Headers;
            var logger = context.GetLogger("FunctionApp.Function6");

            var cookies = req.Cookies;
            cookies.ToList().ForEach(c => logger.LogInformation(c.Name));

            var response = req.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");
            response.WriteString("Successfully Logged Cookies.");

            return response;
        }
    }
}
