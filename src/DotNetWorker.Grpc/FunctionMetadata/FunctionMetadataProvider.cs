// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{ 
    internal class FunctionMetadataProvider : IFunctionMetadataProvider
    {
        private readonly string _directory;
        private const string FileName = "functions.metadata";

        public FunctionMetadataProvider(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public virtual async Task<ImmutableArray<RpcFunctionMetadata>> GetFunctionMetadataAsync()
        {
            string metadataFile = Path.Combine(_directory, FileName);

            if (File.Exists(metadataFile))
            {
                using (var fs = File.OpenRead(metadataFile))
                {
                    // deserialize as json element to preserve raw bindings
                    var jsonMetadataList = await JsonSerializer.DeserializeAsync<JsonElement>(fs);

                    var functionMetadataResults= new List<RpcFunctionMetadata>(jsonMetadataList.GetArrayLength());

                    var options = new JsonSerializerOptions();
                    options.PropertyNameCaseInsensitive = true;

                    foreach (var jsonMetadata in jsonMetadataList.EnumerateArray())
                    {
                        var functionMetadata = JsonSerializer.Deserialize<RpcFunctionMetadata>(jsonMetadata.GetRawText(), options);

                        if (functionMetadata is null)
                        {
                            throw new NullReferenceException("Function metadata could not be found.");
                        }

                        // hard-coded values that are checked for when the host validates functions
                        functionMetadata.IsProxy = false;
                        functionMetadata.Language = "dotnet-isolated";

                        var rawBindings = GetRawBindings(jsonMetadata);

                        foreach (var binding in rawBindings.EnumerateArray())
                        {
                            functionMetadata.RawBindings.Add(binding.GetRawText());

                            BindingInfo bindingInfo = CreateBindingInfo(binding);

                            binding.TryGetProperty("name", out JsonElement jsonName);

                            functionMetadata.Bindings.Add(jsonName.ToString(), bindingInfo);
                        }

                        functionMetadataResults.Add(functionMetadata);
                    }

                    return functionMetadataResults.ToImmutableArray();
                }
            }

            return ImmutableArray<RpcFunctionMetadata>.Empty;
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

        internal static BindingInfo CreateBindingInfo(JsonElement binding)
        {
            var hasDirection = binding.TryGetProperty("direction", out JsonElement jsonDirection);
            var hasType = binding.TryGetProperty("type", out JsonElement jsonType);

            if (!hasDirection || !hasType)
            {
                throw new FormatException("Bindings must declare a direction and type.");
            }

            BindingInfo bindingInfo = new BindingInfo
            {
                Direction = Enum.Parse<BindingInfo.Types.Direction>(jsonDirection.ToString()!),
                Type = jsonType.ToString()
            };

            var hasDataType = binding.TryGetProperty("dataType", out JsonElement jsonDataType);

            if (hasDataType)
            {
                bindingInfo.DataType = Enum.Parse<BindingInfo.Types.DataType>(jsonDataType.ToString()!);
            }

            return bindingInfo;
        }
    }
}
