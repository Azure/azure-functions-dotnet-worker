﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    internal class WebPubSubContextJsonConverter : JsonConverter<WebPubSubContext>
    {
        public override WebPubSubContext? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var innerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var element = jsonDoc.RootElement;
    
            var isPreflight = element.GetProperty("isPreflight").GetBoolean();
            WebPubSubEventRequest request;
    
            if (isPreflight)
            {
                 request = JsonSerializer.Deserialize<PreflightRequest>(element.GetProperty("request").GetRawText());
            }
            else
            {
                // depends on connectionContext info to parse request.
                var connectionContext = JsonSerializer.Deserialize<WebPubSubConnectionContext>(element.GetProperty("request").GetProperty("connectionContext").GetRawText(), innerOptions);
                if (connectionContext.EventType == WebPubSubEventType.User)
                {
                    request = JsonSerializer.Deserialize<UserEventRequest>(element.GetProperty("request").GetRawText());
                }
                else if (connectionContext.EventName == "connect")
                {
                    request = JsonSerializer.Deserialize<ConnectEventRequest>(element.GetProperty("request").GetRawText());
                }
                else if (connectionContext.EventName == "disconnected")
                {
                    request = JsonSerializer.Deserialize<DisconnectedEventRequest>(element.GetProperty("request").GetRawText());
                }
                else if (connectionContext.EventName == "connected")
                {
                    request = JsonSerializer.Deserialize<ConnectedEventRequest>(element.GetProperty("request").GetRawText());
                }
                else
                {
                    throw new ArgumentException($"Not supported event. EventType: {connectionContext.EventType}, EventName: {connectionContext.EventName}.");
                }
            }
    
            // according to property names to detect 
            return new WebPubSubContext
            {
                Request = request,
                Response = JsonSerializer.Deserialize<SimpleResponse>(element.GetProperty("response").GetRawText()),
                HasError = element.GetProperty("hasError").GetBoolean(),
                ErrorMessage = element.GetProperty("errorMessage").GetString(),
                IsPreflight = isPreflight
            };
        }
    
        public override void Write(Utf8JsonWriter writer, WebPubSubContext value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
