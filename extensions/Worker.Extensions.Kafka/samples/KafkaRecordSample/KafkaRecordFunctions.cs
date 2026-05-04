// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KafkaRecordSample
{
    public static class KafkaRecordFunctions
    {
        private static readonly ConcurrentQueue<ReceivedRecord> ReceivedRecords = new();

        /// <summary>
        /// HTTP trigger that produces a message to Kafka.
        /// GET/POST /api/produce?message=hello
        /// </summary>
        [Function("Produce")]
        [KafkaOutput("LocalBroker", "%KafkaRecordSampleTopic%")]
        public static string Produce(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "produce")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("Produce");
            var message = req.Query["message"] ?? $"kafkarecord-sample-{System.Guid.NewGuid():N}";
            logger.LogInformation("Producing message: {Message}", message);
            return message;
        }

        /// <summary>
        /// Kafka trigger that receives a KafkaRecord (deferred binding with Protobuf transport).
        /// </summary>
        [Function("Consume")]
        public static void Consume(
            [KafkaTrigger("LocalBroker", "%KafkaRecordSampleTopic%",
                ConsumerGroup = "%KafkaRecordSampleConsumerGroup%")] KafkaRecord record,
            FunctionContext context)
        {
            var logger = context.GetLogger("Consume");
            var value = record.Value != null ? Encoding.UTF8.GetString(record.Value) : "(null)";
            var key = record.Key != null ? Encoding.UTF8.GetString(record.Key) : "(null)";

            var received = new ReceivedRecord
            {
                Topic = record.Topic,
                Partition = record.Partition,
                Offset = record.Offset,
                Key = key,
                Value = value,
                TimestampMs = record.Timestamp?.UnixTimestampMs,
                TimestampType = record.Timestamp?.Type.ToString(),
                HeaderCount = record.Headers?.Length ?? 0,
            };

            ReceivedRecords.Enqueue(received);
            while (ReceivedRecords.Count > 100 && ReceivedRecords.TryDequeue(out _)) { }

            logger.LogInformation(
                "Received KafkaRecord: topic={Topic}, partition={Partition}, offset={Offset}, value={Value}",
                received.Topic, received.Partition, received.Offset, received.Value);
        }

        /// <summary>
        /// HTTP trigger that returns the latest received records.
        /// GET /api/status?message=hello
        /// </summary>
        [Function("Status")]
        public static HttpResponseData Status(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequestData req,
            FunctionContext context)
        {
            var message = req.Query["message"];
            var records = ReceivedRecords.ToArray();
            var match = string.IsNullOrEmpty(message)
                ? records.LastOrDefault()
                : records.LastOrDefault(r => r.Value == message);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(System.Text.Json.JsonSerializer.Serialize(new
            {
                count = records.Length,
                matched = match != null,
                record = match,
            }));
            return response;
        }
    }

    public class ReceivedRecord
    {
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public long? TimestampMs { get; set; }
        public string TimestampType { get; set; }
        public int HeaderCount { get; set; }
    }
}
