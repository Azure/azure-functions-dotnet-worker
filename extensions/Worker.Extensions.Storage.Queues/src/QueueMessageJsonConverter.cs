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
            JObject jo = JObject.Load(reader);
            Connection conn = new Connection
            {
                Id = (string)jo["connectionId"],
                SystemId = (string)jo["systemId"]
            };

            // Construct the Result object using the non-default constructor
            QueueMessage result = new QueueMessage();

            // (If anything else needs to be populated on the result object, do that here)

            // Return the result
            return result;
        }

        public override void Write(Utf8JsonWriter writer, QueueMessage value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}