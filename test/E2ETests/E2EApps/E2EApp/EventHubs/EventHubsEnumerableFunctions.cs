using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.E2EApp.EventHubs;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class EventHubsEnumerableFunctions
    {
        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(EventHubsEnumerableTrigger))]
        [EventHubOutput("test-output-string-dotnet-isolated", Connection = "EventHubConnectionAppSetting")]
        public static TestData EventHubsEnumerableTrigger([EventHubTrigger("test-input-enumerable-dotnet-isolated", Connection = "EventHubConnectionAppSetting")] List<TestData> input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(EventHubsEnumerableTrigger));
            logger.LogInformation($"First trigger (List<TestData>)!!");
            input.ForEach(item => logger.LogInformation(item.ToString()));
            return input[0];
        }

        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(EventHubsVerifyOutputEnumerable))]
        [QueueOutput("test-eventhub-output-string-dotnet-isolated")]
        public static string EventHubsVerifyOutputEnumerable([EventHubTrigger("test-output-enumerable-dotnet-isolated", Connection = "EventHubConnectionAppSetting")] List<string> input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(EventHubsVerifyOutputEnumerable));
            logger.LogInformation($"Second trigger (List<string>)!! '{input[0]}'");
            return input[0];
        }

        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(TestEnumerable))]
        [EventHubOutput("test-input-enumerable-dotnet-isolated", Connection = "EventHubConnectionAppSetting")]
        public static TestData TestEnumerable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(TestEnumerable));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            return new TestData()
            {
                Name = "Ballmer",
                TimeProperty = "2021-01-27T15:57:38.000-06:00"
            };
        }
    }
}
