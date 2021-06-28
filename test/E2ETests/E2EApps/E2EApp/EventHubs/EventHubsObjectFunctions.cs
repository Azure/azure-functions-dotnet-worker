using Microsoft.Azure.Functions.Worker.E2EApp.EventHubs;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class EventHubsObjectFunctionsF
    {
        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(EventHubsObjectFunction))]
        [EventHubOutput("test-eventhub-output-object-dotnet-isolated", Connection = "EventHubConnectionAppSetting")]
        public static TestData EventHubsObjectFunction([EventHubTrigger("test-eventhub-input-object-dotnet-isolated", Connection = "EventHubConnectionAppSetting", IsBatched = false)] TestData input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(EventHubsObjectFunction));
            logger.LogInformation($"First Trigger (TestData)!! Name: '{input.Name}' Time: '{input.TimeProperty}'");
            return input;
        }

        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(EventHubsVerifyOutputObject))]
        [QueueOutput("test-eventhub-output-object-dotnet-isolated")]
        public static TestData EventHubsVerifyOutputObject([EventHubTrigger("test-eventhub-output-object-dotnet-isolated", Connection = "EventHubConnectionAppSetting", IsBatched = false)] TestData input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(EventHubsVerifyOutputObject));
            logger.LogInformation($"Second Trigger (TestData)!! Name: '{input.Name}' Time: '{input.TimeProperty}'");
            return input;
        }

        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(TestObject))]
        [EventHubOutput("test-eventhub-input-object-dotnet-isolated", Connection = "EventHubConnectionAppSetting")]
        public static TestData TestObject(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(TestObject));
            return new TestData()
            {
                Name = "Ballmer",
                TimeProperty = "2021-01-27T15:57:38.000-09:00"
            };
        }
    }
}
