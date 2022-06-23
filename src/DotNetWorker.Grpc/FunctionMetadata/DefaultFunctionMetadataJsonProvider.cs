using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata
{
    internal class DefaultFunctionMetadataJsonProvider : IFunctionMetadataJsonProvider
    {
        private const string FileName = "functions.metadata";
        private JsonSerializerOptions deserializationOptions;

        public DefaultFunctionMetadataJsonProvider()
        {
            deserializationOptions = new JsonSerializerOptions();
            deserializationOptions.PropertyNameCaseInsensitive = true;
        }


        public async Task<ImmutableArray<JsonElement>> GetFunctionMetadataJsonAsync(string directory)
        {
            string metadataFile = Path.Combine(directory, FileName);

            if (!File.Exists(metadataFile))
            {
                throw new FileNotFoundException($"Function metadata file not found. File path used:{metadataFile}");
            }

            using (var fs = File.OpenRead(metadataFile))
            {
                // deserialize as json element to preserve raw bindings
                var jsonMetadataList = await JsonSerializer.DeserializeAsync<List<JsonElement>>(fs);

                if (jsonMetadataList is null || jsonMetadataList.Count == 0)
                {
                    throw new NullReferenceException("Function metadata could not be found.");
                }

                return jsonMetadataList.ToImmutableArray();
            }
        }
    }
}
