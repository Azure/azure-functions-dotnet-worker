// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public class CancellationHttpFunctions(ILogger<CancellationHttpFunctions> logger)
    {
        private readonly ILogger<CancellationHttpFunctions> _logger = logger;

        [Function(nameof(HttpWithCancellationTokenNotUsed))]
        public async Task<IActionResult> HttpWithCancellationTokenNotUsed(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("HttpWithCancellationTokenNotUsed processed a request.");

            await SimulateWork(CancellationToken.None);

            return new OkObjectResult("Processing completed successfully.");
        }

        [Function(nameof(HttpWithCancellationTokenIgnored))]
        public async Task<IActionResult> HttpWithCancellationTokenIgnored(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("HttpWithCancellationTokenIgnored processed a request.");

            await SimulateWork(cancellationToken);

            return new OkObjectResult("Processing completed successfully.");
        }

        [Function(nameof(HttpWithCancellationTokenHandled))]
        public async Task<IActionResult> HttpWithCancellationTokenHandled(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("HttpWithCancellationTokenHandled processed a request.");

            try
            {
                await SimulateWork(cancellationToken);

                return new OkObjectResult("Processing completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request was cancelled.");

                // Take precautions like noting how far along you are with processing the batch
                await Task.Delay(1000);

                return new ObjectResult(new { statusCode = StatusCodes.Status499ClientClosedRequest, message = "Request was cancelled." });
            }
        }

        private async Task SimulateWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting work...");

            for (int i = 0; i < 5; i++)
            {
                // Simulate work
                await Task.Delay(1000, cancellationToken);
                _logger.LogWarning($"Work iteration {i + 1} completed.");
            }

            _logger.LogInformation("Work completed.");
        }
    }
}
