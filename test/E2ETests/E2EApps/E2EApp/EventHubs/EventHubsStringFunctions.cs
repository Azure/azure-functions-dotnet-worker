using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class EventHubsStringFunctions
    {
        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(EventHubsStringTrigger))]
        [EventHubOutput("test-output-string-dotnet-isolated", Connection = "EventHubConnectionAppSetting")]
        public static string EventHubsStringTrigger([EventHubTrigger("test-input-string-dotnet-isolated", Connection = "EventHubConnectionAppSetting")] string[] input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(EventHubsStringTrigger));
            logger.LogInformation($"First trigger (string[])!! '{input[0]}'");
            return input[0];
        }

        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(EventHubsVerifyOutputString))]
        [QueueOutput("test-eventhub-output-string-dotnet-isolated")]
        public static string EventHubsVerifyOutputString([EventHubTrigger("test-output-string-dotnet-isolated", Connection = "EventHubConnectionAppSetting")] string[] input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(EventHubsVerifyOutputString));
            logger.LogInformation($"Second trigger (string[])!! '{input[0]}'");
            return input[0];
        }

        // TODO: We need to enable Event Hubs tests.
        // [Function(nameof(Test))]
        [EventHubOutput("test-input-string-dotnet-isolated", Connection = "EventHubConnectionAppSetting")]
        public static string Test(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Test));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            return "hello world";
        }
    }
}
