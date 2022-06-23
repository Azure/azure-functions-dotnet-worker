using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata
{
    internal class FunctionMetadataConverter
    {
        // this  method assumes that we receive JsonElements in the structure of a functions.metadata file
        internal static ImmutableArray<RpcFunctionMetadata> ToRpcFunctionMetadata(ICollection<JsonElement> jsonMetadataList)
        {
            var deserializationOptions = new JsonSerializerOptions();
            deserializationOptions.PropertyNameCaseInsensitive = true;

            var functionMetadataResults = new List<RpcFunctionMetadata>(jsonMetadataList.Count);

            foreach (var jsonMetadata in jsonMetadataList)
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

                    BindingInfo bindingInfo = CreateBindingInfo(binding);

                    binding.TryGetProperty("name", out JsonElement jsonName);

                    functionMetadata.Bindings.Add(jsonName.ToString(), bindingInfo);
                }

                functionMetadataResults.Add(functionMetadata);
            }

            return functionMetadataResults.ToImmutableArray();

        }

        internal static JsonElement GetRawBindings(JsonElement jsonMetadata)
        {
            jsonMetadata.TryGetProperty("bindings", out JsonElement bindingsJson);

            if (bindingsJson.GetArrayLength() == 0)
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

            if (!hasDirection
                || !hasType
                || !Enum.TryParse(jsonDirection.ToString()!, out BindingInfo.Types.Direction direction))
            {
                throw new FormatException("Bindings must declare a direction and type.");
            }

            BindingInfo bindingInfo = new BindingInfo
            {
                Direction = direction,
                Type = jsonType.ToString()
            };

            var hasDataType = binding.TryGetProperty("dataType", out JsonElement jsonDataType);

            if (hasDataType)
            {
                if (!Enum.TryParse(jsonDataType.ToString()!, out BindingInfo.Types.DataType dataType))
                {
                    throw new FormatException("Invalid DataType for a binding.");
                }

                bindingInfo.DataType = dataType;
            }

            return bindingInfo;
        }
    }
}
