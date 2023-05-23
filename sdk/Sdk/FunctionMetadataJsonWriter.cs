// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal static class FunctionMetadataJsonWriter
    {
        private const string FileName = "functions.metadata";
        private static readonly JsonSerializerOptions s_serializerOptions = CreateSerializerOptions();

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var namingPolicy = new FunctionsJsonNamingPolicy();
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyProperties = true,
                DictionaryKeyPolicy = namingPolicy,
                PropertyNamingPolicy = namingPolicy,
                Converters =
                {
                    new JsonStringEnumConverter(),
                }
            };
        }

        public static void WriteMetadata(IEnumerable<SdkFunctionMetadata> functions, string metadataFileDirectory)
        {
            string metadataFile = Path.Combine(metadataFileDirectory, FileName);
            using var fs = new FileStream(metadataFile, FileMode.Create, FileAccess.Write);
            using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
            JsonSerializer.Serialize(writer, functions, s_serializerOptions);
        }
    }
}
