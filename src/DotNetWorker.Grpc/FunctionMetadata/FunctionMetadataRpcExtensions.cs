using System;
using System.Text.Json;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata
{
    internal static class FunctionMetadataRpcExtensions
    {
        internal static MapField<string, BindingInfo> GetBindingInfoList(this IFunctionMetadata funcMetadata)
        {
            if (funcMetadata is RpcFunctionMetadata rpcFuncMetadata)
            {
                return rpcFuncMetadata.Bindings;
            }

            MapField<string, BindingInfo> bindings = new MapField<string, BindingInfo>();
            var rawBindings = funcMetadata.RawBindings;

            if(rawBindings is null || rawBindings.Count == 0)
            {
                throw new FormatException("At least one binding must be declared in a Function.");
            }

            foreach (var bindingJson in rawBindings)
            {
                var binding = JsonSerializer.Deserialize<JsonElement>(bindingJson);
                BindingInfo bindingInfo = CreateBindingInfo(binding);
                binding.TryGetProperty("name", out JsonElement jsonName);
                bindings.Add(jsonName.ToString()!, bindingInfo);
            }

            return bindings;
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
