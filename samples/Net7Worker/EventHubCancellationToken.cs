// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Net7Worker
{
    public class EventHubCancellationToken
    {
        private readonly ILogger _logger;

        public EventHubCancellationToken(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventHubCancellationToken>();
        }

        [Function(nameof(EventHubCancellationToken))]
        public void Run(
            [EventHubTrigger("src", Connection = "EventHubConnection")] string[] messages,
            FunctionContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("C# EventHub trigger function processing a request.");

            try
            {
                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("A cancellation token was received. Taking precautionary actions.");
                        // Take precautions like noting how far along you are with processing the batch
                        _logger.LogInformation("Precautionary activities --complete--.");
                        break;
                    }
                    else
                    {
                        // Business logic as usual
                        _logger.LogInformation($"Message: {message} was processed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something unexpected happened: {ex.Message}");
            }
        }
    }
}
