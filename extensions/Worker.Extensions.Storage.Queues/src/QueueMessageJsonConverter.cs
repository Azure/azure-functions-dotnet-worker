// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Queues.Models;

namespace Microsoft.Azure.Functions.Worker.Storage.Queues
{
    public class QueueMessageJsonConverter : JsonConverter<QueueMessage>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(QueueMessage);
        }

        public override QueueMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonElement root = JsonDocument.ParseValue(ref reader).RootElement;

            root.TryGetProperty("MessageId", out var messageId);
            root.TryGetProperty("PopReceipt", out var popReceipt);
            root.TryGetProperty("DequeueCount", out var dequeueCount);
            root.TryGetProperty("NextVisibleOn", out var nextVisibleOn);
            root.TryGetProperty("ExpiresOn", out var expiresOn);
            root.TryGetProperty("MessageText", out var messageText);

            return QueuesModelFactory.QueueMessage(
                messageId.GetString(),
                popReceipt.GetString(),
                messageText.GetString(),
                dequeueCount.GetInt64(),
                nextVisibleOn.GetDateTimeOffset(),
                expiresOn.GetDateTimeOffset()
            );
        }

        public override void Write(Utf8JsonWriter writer, QueueMessage value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}