// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class HttpTriggerWithCancellation
    {
        private readonly ILogger _logger;

        public HttpTriggerWithCancellation(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTriggerWithCancellation>();
        }

        [Function(nameof(HttpTriggerWithCancellation))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,"get", "post", Route = null)]
            HttpRequestData req,
            FunctionContext executionContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("HttpTriggerWithCancellation function triggered");

            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteString($"Hello world!");

                await Task.Delay(5000, cancellationToken);

                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Function invocation cancelled");

                var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                response.WriteString("Invocation cancelled");

                return response;
            }
        }
    }
}