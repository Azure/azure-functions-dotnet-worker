using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    internal class SampleGenFunctionMetadtaJsonProvider : IFunctionMetadataJsonProvider
    {
        public Task<ImmutableArray<JsonElement>> GetFunctionMetadataJsonAsync(string directory)
        {
            var metadataList = new List<JsonElement>();

            var HttpTriggerSimple = new
            {
                name = "HttpTriggerSimple",
                scriptFile = "FunctionApp.dll",
                language = "dotnet-isolated",
                entryPoint = "FunctionApp.HttpTriggerSimple.Run",
                isProxy = false,
                bindings = new List<Object>()
            };

            var req = new
            {
                name = "req",
                type = "HttpTrigger",
                direction = "In",
                authLevel = Enum.GetName(typeof(AuthorizationLevel), 0), // probably need to store this info in the source generator b/c if I do this here then I need to import the type. probably?
                methods = new List<string> { "get", "post" },
            };

            HttpTriggerSimple.bindings.Add(req);

            // need to add this automatically for some functions
            var returnBinding = new
            {
                name = "$return",
                type = "http",
                direction = "Out",
            };

            HttpTriggerSimple.bindings.Add(returnBinding);

            var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(HttpTriggerSimple, new JsonSerializerOptions());

            metadataList.Add(JsonSerializer.Deserialize<JsonElement>(jsonUtf8Bytes));

            return Task.FromResult(metadataList.ToImmutableArray());

        }

        //Enum used to specify the authorization level for http functions.
        public enum AuthorizationLevel
        {
            Anonymous,
            User,
            Function,
            System,
            Admin
        }
    }
}
