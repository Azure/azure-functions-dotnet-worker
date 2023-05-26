// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Queues.Models;

namespace Microsoft.Azure.Functions.Worker.Storage.Queues
{
    internal class QueueMessageJsonConverter : JsonConverter<QueueMessage>
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(QueueMessage);

        public override QueueMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is not JsonTokenType.StartObject)
            {
                throw new JsonException("JSON payload expected to start with StartObject token.");
            }

            string messageId = String.Empty;
            string popReceipt = String.Empty;
            string messageText = String.Empty;
            long dequeueCount = 1;
            DateTime? nextVisibleOn = null;
            DateTime? insertedOn = null;
            DateTime? expiresOn = null;

            var startDepth = reader.CurrentDepth;

            while (reader.Read())
            {
                if (reader.TokenType is JsonTokenType.EndObject && reader.CurrentDepth == startDepth)
                {
                    return QueuesModelFactory.QueueMessage(
                        messageId,
                        popReceipt,
                        messageText,
                        dequeueCount,
                        nextVisibleOn,
                        insertedOn,
                        expiresOn
                    );
                }

                if (reader.TokenType is not JsonTokenType.PropertyName)
                {
                    continue;
                }

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "messageid":
                        messageId = reader.GetString() ?? throw new JsonException("JSON payload must contain a MessageId.");
                        break;
                    case "popreceipt":
                        popReceipt = reader.GetString() ?? throw new JsonException("JSON payload must contain a PopReceipt.");
                        break;
                    case "messagetext":
                        messageText = reader.GetString() ?? throw new JsonException("JSOn payload must contain a MessageText.");
                        break;
                    case "dequeuecount":
                        dequeueCount = reader.GetInt64();
                        break;
                    case "nextvisibleon":
                        nextVisibleOn = reader.GetDateTime();
                        break;
                    case "insertedon":
                        insertedOn = reader.GetDateTime();
                        break;
                    case "expireson":
                        expiresOn = reader.GetDateTime();
                        break;
                    default:
                        break;
                }
            }

            throw new JsonException("JSON payload expected to end with EndObject token.");
        }

        public override void Write(Utf8JsonWriter writer, QueueMessage value, JsonSerializerOptions options)
        {
            throw new JsonException($"Serialization is not supported by the {nameof(QueueMessageJsonConverter)}.");
        }
    }
}
