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
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(QueueMessage);
        }

        public override QueueMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string messageId = "";
            string popReceipt = "";
            string messageText = "";
            long dequeueCount = 1;
            DateTime? nextVisibleOn = null;
            DateTime? insertedOn = null;
            DateTime? expiresOn = null;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("JSON payload expected to start with StartObject token.");
            }

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

                switch (propertyName)
                {
                    // We're not expecting these three values to be null. If they are, should we throw or just return an empty string?
                    case "MessageId":
                        messageId = reader.GetString() ?? throw new ArgumentNullException("MessageId");
                        break;
                    case "PopReceipt":
                        popReceipt = reader.GetString() ?? throw new ArgumentNullException("PopReceipt");
                        break;
                    case "MessageText":
                        messageText = reader.GetString() ?? throw new ArgumentNullException("MessageText");
                        break;
                    case "DequeueCount":
                        dequeueCount = reader.GetInt64();
                        break;
                    case "NextVisibleOn":
                        nextVisibleOn = reader.GetDateTime();
                        break;
                    case "InsertedOn":
                        insertedOn = reader.GetDateTime();
                        break;
                    case "ExpiresOn":
                        expiresOn = reader.GetDateTime();
                        break;
                    default:
                        break;
                }
            }

            throw new JsonException("JSON payload expected to start with EndObject token.");
        }

        public override void Write(Utf8JsonWriter writer, QueueMessage value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}