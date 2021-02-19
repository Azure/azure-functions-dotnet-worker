// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class EventGridFunction
    {
        [Function("EventGridFunction")]
        [EventGridOutput("output", TopicEndpointUri = "MyEventGridTopicUriSetting", TopicKeySetting = "MyEventGridTopicKeySetting")]
        public static void Run([EventGridTrigger] MyEventType input, FunctionContext context)
        {
            var logger = context.GetLogger("EventGridFunction");

            logger.LogInformation(input.Data.ToString());

            var outputEvent = new MyEventType()
            {
                Id = "unique-id",
                Subject = "abc-subject",
                Data = new Dictionary<string, object>
                {
                    { "myKey", "myValue" }
                }
            };

            context.OutputBindings["output"] = outputEvent;
        }
    }

    public class MyEventType
    {
        public string Id { get; set; }

        public string Topic { get; set; }

        public string Subject { get; set; }

        public int EventType { get; set; }

        public IDictionary<string, object> Data { get; set; }
    }
}
