// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;

namespace SampleApp
{
    public static class EventHubsTriggerMetadata
    {
        [Function("EventHubsTriggerMetadata-Context")]
        public static void UsingContext([EventHubTrigger("src-context", Connection = "EventHubConnectionAppSetting")] string[] messages, FunctionContext context)
        {
            // Properties for messages are passed as binding data, which is accessible via the FunctionContext.
            // However, this requires converting manually into the correct types.            
            var enqueuedTimeUtc = context.BindingContext.BindingData["enqueuedTimeUtcArray"];
            var sequenceNumberArray = context.BindingContext.BindingData["sequenceNumberArray"];
            var offsetArray = context.BindingContext.BindingData["offsetArray"];
            var propertiesArray = context.BindingContext.BindingData["propertiesArray"];
            var systemPropertiesArray = context.BindingContext.BindingData["systemPropertiesArray"];
        }

        [Function("EventHubsTriggerMetadata-Parameters")]
        public static void UsingParameters([EventHubTrigger("src-parameters", Connection = "EventHubConnectionAppSetting")] string[] messages,
            DateTime[] enqueuedTimeUtcArray,
            long[] sequenceNumberArray,
            string[] offsetArray,
            Dictionary<string, JsonElement>[] propertiesArray,
            Dictionary<string, JsonElement>[] systemPropertiesArray)
        {
            // You can directly access binding data via parameters.
            for (int i = 0; i < messages.Length; i++)
            {
                string message = messages[i];
                DateTime enqueuedTimeUtc = enqueuedTimeUtcArray[i];
                long sequenceNumber = sequenceNumberArray[i];
                string offset = offsetArray[i];

                // Note: The values in these dictionaries are sent to the worker as JSON. By default, System.Text.Json will not automatically infer primitive values
                //       if you attempt to deserialize to 'object'. See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-5-0#deserialization-of-object-properties       
                //       
                //       If you want to use Dictionary<string, object> and have the values automatically inferred, you can use the sample JsonConverter specified
                //       here: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
                //
                //       See the Configuration sample in this repo for details on how to add this custom converter to the JsonSerializerOptions, or for 
                //       details on how to use Newtonsoft.Json, which does automatically infer primitive values.
                Dictionary<string, JsonElement> properties = propertiesArray[i];
                Dictionary<string, JsonElement> systemProperties = systemPropertiesArray[i];
            }
        }
    }
}
