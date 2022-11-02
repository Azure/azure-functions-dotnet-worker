// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Net7Worker
{
    public class HttpFunction
    {
        private readonly ILogger _logger;

        public HttpFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpFunction>();
        }

        [Function(nameof(HttpFunction))]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,"get", "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("C# HTTP trigger function processing a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("Welcome to Azure Functions - Isolated .NET 7!");

            return response;
        }
    }
}
