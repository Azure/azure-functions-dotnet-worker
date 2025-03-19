// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
            string newContent = JsonSerializer.Serialize(functions, s_serializerOptions);
            if (TryReadFile(metadataFile, out string? current) && string.Equals(current, newContent, StringComparison.Ordinal))
            {
                // Incremental build support. Skip writing if the content is the same.
                return;
            }

            File.WriteAllText(metadataFile, newContent);
        }

        private static bool TryReadFile(string filePath, out string? content)
        {
            if (File.Exists(filePath))
            {
                content = File.ReadAllText(filePath);
                return true;
            }

            content = null;
            return false;
        }
    }
}
