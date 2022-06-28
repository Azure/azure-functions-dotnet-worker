﻿using System;
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
            if (funcMetadata.GetType() == typeof(RpcFunctionMetadata))
            {
                RpcFunctionMetadata rpcFuncMetadata = (RpcFunctionMetadata) funcMetadata;
                return rpcFuncMetadata.Bindings;
            }

            MapField<string, BindingInfo> bindings = new MapField<string, BindingInfo>();
            var rawBindings = funcMetadata.RawBindings;

            foreach (var bindingJson in rawBindings)
            {
                var binding = JsonSerializer.Deserialize<JsonElement>(bindingJson);
                BindingInfo bindingInfo = CreateBindingInfo(binding);
                binding.TryGetProperty("name", out JsonElement jsonName);
                bindings.Add(jsonName.ToString()!, bindingInfo);
            }

            return bindings;
        }

        private static BindingInfo CreateBindingInfo(JsonElement binding)
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
