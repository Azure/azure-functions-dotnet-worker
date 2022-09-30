// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
            FunctionContext executionContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("Welcome to Azure Functions - Isolated .NET 7!");
                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("A cancellation token was received. Taking precautionary actions.");

                // Take precautions like noting how far along you are with processing the batch

                var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("Invocation cancelled, precautionary actions taken.");
                return response;
            }
        }
    }
}