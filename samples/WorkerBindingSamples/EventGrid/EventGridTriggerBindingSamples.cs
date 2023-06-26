﻿// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
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

        [Function("MyEventFunction")]
        public void MyEventFunction([EventGridTrigger] MyEvent input)
        {
            _logger.LogInformation(input.Data?.ToString());
        }

        [Function("CloudEventFunction")]
        public void CloudEventFunction([EventGridTrigger] CloudEvent input)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", input.Type, input.Subject);
        }

        [Function("MultipleCloudEventFunction")]
        public void MultipleCloudEventFunction([EventGridTrigger(IsBatched = true)] CloudEvent[] input)
        {
            for (var i = 0; i < input.Length; i++)
            {
                var cloudEvent = input[i];
                _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
            }
        }

        [Function("EventGridEvent")]
        public void EventGridEvent([EventGridTrigger] EventGridEvent input)
        {
            _logger.LogInformation("Event received: {event}", input.Data.ToString());
        }

        [Function("EventGridEventArray")]
        public void EventGridEventArray([EventGridTrigger(IsBatched = true)] EventGridEvent[] input)
        {
            for (var i = 0; i < input.Length; i++)
            {
                var eventGridEvent = input[i];
                _logger.LogInformation("Event received: {event}", eventGridEvent.Data.ToString());
            }
        }

        [Function("BinaryDataEvent")]
        public void BinaryDataEvent([EventGridTrigger] BinaryData input)
        {
            _logger.LogInformation("Event received: {event}", input.ToString());
        }

        [Function("BinaryDataArrayEvent")]
        public void BinaryDataArrayEvent([EventGridTrigger(IsBatched = true)] BinaryData[] input)
        {
            for (var i = 0; i < input.Length; i++)
            {
                var binaryDataEvent = input[i];
                _logger.LogInformation("Event received: {event}", binaryDataEvent.ToString());
            }
        }

        [Function("StringArrayEvent")]
        public void StringArrayEvent([EventGridTrigger(IsBatched = true)] string[] input)
        {
            for (var i = 0; i < input.Length; i++)
            {
                var stringEventGrid = input[i];
                _logger.LogInformation("Event received: {event}", stringEventGrid);
            }
        }

        [Function("StringEvent")]
        public void StringEvent([EventGridTrigger] string input)
        {
            _logger.LogInformation("Event received: {event}", input);
        }
    }

    public class MyEvent
    {
        public string? Id { get; set; }

        public string? Topic { get; set; }

        public string? Subject { get; set; }

        public string? EventType { get; set; }

        public DateTime EventTime { get; set; }

        public object? Data { get; set; }
    }
}
