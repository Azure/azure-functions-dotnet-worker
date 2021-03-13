// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CustomMiddleware
{
    public class HttpFunction
    {
        [Function("HttpFunction")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger<HttpFunction>();

            // Get the item set by the middleware
            if (context.Items.TryGetValue("middlewareitem", out object value) && value is string message)
            {
                logger.LogInformation("From middleware: {message}", message);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            // Set a context item the middleware can retrieve
            context.Items.Add("functionitem", "Hello from function!");

            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("Welcome to .NET 5!!");

            return response;
        }
    }
}
