// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionMetadataProvider : IFunctionMetadataProvider
    {
        private const string FileName = "functions.metadata";
        private JsonSerializerOptions deserializationOptions;

        public DefaultFunctionMetadataProvider()
        {
            deserializationOptions = new JsonSerializerOptions();
            deserializationOptions.PropertyNameCaseInsensitive = true;
        }

        public virtual async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            string metadataFile = Path.Combine(directory, FileName);

            if (!File.Exists(metadataFile))
            {
                throw new FileNotFoundException($"Function metadata file not found. File path used:{metadataFile}");
            }

            using (var fs = File.OpenRead(metadataFile))
            {
                // deserialize as json element to preserve raw bindings
                var jsonMetadataList = await JsonSerializer.DeserializeAsync<JsonElement>(fs);

                var functionMetadataResults = new List<IFunctionMetadata>(jsonMetadataList.GetArrayLength());

                foreach (var jsonMetadata in jsonMetadataList.EnumerateArray())
                {
                    var functionMetadata = JsonSerializer.Deserialize<RpcFunctionMetadata>(jsonMetadata.GetRawText(), deserializationOptions);

                    if (functionMetadata is null)
                    {
                        throw new NullReferenceException("Function metadata could not be found.");
                    }

                    // hard-coded values that are checked for when the host validates functions
                    functionMetadata.IsProxy = false;
                    functionMetadata.Language = "dotnet-isolated";
                    functionMetadata.FunctionId = Guid.NewGuid().ToString();

                    var rawBindings = GetRawBindings(jsonMetadata);

                    foreach (var binding in rawBindings.EnumerateArray())
                    {
                        functionMetadata.RawBindings.Add(binding.GetRawText());
                    }

                    functionMetadataResults.Add(functionMetadata);
                }

                return functionMetadataResults.ToImmutableArray();
            }
        }

        internal static JsonElement GetRawBindings(JsonElement jsonMetadata)
        {
            jsonMetadata.TryGetProperty("bindings", out JsonElement bindingsJson);

            if(bindingsJson.GetArrayLength() == 0)
            {
                var funcName = jsonMetadata.GetProperty("name");
                throw new FormatException($"At least one binding must be declared in function `{funcName}`");
            }

            return bindingsJson;
        }
    }
}
