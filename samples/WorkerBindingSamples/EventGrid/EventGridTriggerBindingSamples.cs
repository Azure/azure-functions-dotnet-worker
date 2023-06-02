// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WorkerBindingSamples.EventGrid
{
    public class EventGridTriggerBindingSamples
    {
        private readonly ILogger _logger;

        public EventGridTriggerBindingSamples(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventGridTriggerBindingSamples>();
        }

        [Function("Function")]
        public void Run([EventGridTrigger] MyEvent input)
        {
            _logger.LogInformation(input.Data.ToString());
        }

        [Function("CloudEventFunction")]
        public void Run([EventGridTrigger] CloudEvent input)
        {
            _logger.LogInformation("Event received " + input.Type + " " + input.Subject);
        }
    }

    public class MyEvent
    {
        public string Id { get; set; }

        public string Topic { get; set; }

        public string Subject { get; set; }

        public string EventType { get; set; }

        public DateTime EventTime { get; set; }

        public object Data { get; set; }
    }
}
