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

        // Sample showing how to handle a cancellation token being received
        // In this example, the function invocation status will be "Cancelled"
        //<docsnippet_cancellation_token_throw>
        [Function(nameof(ThrowOnCancellation))]
        public async Task ThrowOnCancellation(
            [EventHubTrigger("sample-workitem-1", Connection = "EventHubConnection")] string[] messages,
            FunctionContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("C# EventHub {functionName} trigger function processing a request.", nameof(ThrowOnCancellation));

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(6000); // task delay to simulate message processing
                _logger.LogInformation("Message '{msg}' was processed.", message);
            }
        }
        //</docsnippet_cancellation_token_throw>

        // Sample showing how to take precautionary/clean up actions if a cancellation token is received
        // In this example, the function invocation status will be "Successful"
        //<docsnippet_cancellation_token_cleanup>
        [Function(nameof(HandleCancellationCleanup))]
        public async Task HandleCancellationCleanup(
            [EventHubTrigger("sample-workitem-2", Connection = "EventHubConnection")] string[] messages,
            FunctionContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("C# EventHub {functionName} trigger function processing a request.", nameof(HandleCancellationCleanup));

            foreach (var message in messages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("A cancellation token was received, taking precautionary actions.");
                    // Take precautions like noting how far along you are with processing the batch
                    _logger.LogInformation("Precautionary activities complete.");
                    break;
                }

                await Task.Delay(6000); // task delay to simulate message processing
                _logger.LogInformation("Message '{msg}' was processed.", message);
            }
        }
        //</docsnippet_cancellation_token_cleanup>
    }
}
