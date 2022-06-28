using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Using this for development and debugging only
    /// </summary>
    public class SampleFunctionMetadataProvider : IFunctionMetadataProvider
    {
        /// <inheritdoc/>
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var funcMetadataList = new List<IFunctionMetadata>();
            
            var scriptFile = "FunctionApp.dll";

            var func1RawBindings = new List<string>();
            var func1EntryPoint = "FunctionApp.HttpTriggerSimple.Run";

            var req = new
            {
                name = "req",
                type = "HttpTrigger",
                direction = "In",
                authLevel = Enum.GetName(typeof(AuthorizationLevel), 0), // probably need to store this info in the source generator b/c if I do this here then I need to import the type. probably?
                methods = new List<string> { "get", "post" },
            };

            var reqAsJsonString = JsonSerializer.Serialize(req).ToString();
            func1RawBindings.Add(reqAsJsonString);

            // need to add this automatically for some functions
            var returnBinding = new
            {
                name = "$return",
                type = "http",
                direction = "Out",
            };

            var returnBindingAsJsonString = JsonSerializer.Serialize(returnBinding).ToString();
            func1RawBindings.Add(returnBindingAsJsonString);

            var func1 = new DefaultFunctionMetadata(Guid.NewGuid().ToString(), "dotnet-isolated", "HttpTriggerSimple", func1EntryPoint, func1RawBindings, scriptFile);

            funcMetadataList.Add(func1);

            return Task.FromResult(funcMetadataList.ToImmutableArray());
        }

        /// <summary>
        /// Enum used to specify the authorization level for http functions.
        /// </summary>
        internal enum AuthorizationLevel
        {
            Anonymous,
            User,
            Function,
            System,
            Admin
        }
    }
}
