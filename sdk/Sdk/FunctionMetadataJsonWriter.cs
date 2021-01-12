using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal static class FunctionMetadataJsonWriter
    {
        private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();
        private const string _fileName = "functions.metadata";

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyProperties = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        public static void WriteMetadata(IEnumerable<SdkFunctionMetadata> functions, string metadataFileDirectory)
        {
            string metadataFile = Path.Combine(metadataFileDirectory, _fileName);

            using (var fs = new FileStream(metadataFile, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
                {
                    JsonSerializer.Serialize(writer, functions, _serializerOptions);
                }
            }
        }
    }
}