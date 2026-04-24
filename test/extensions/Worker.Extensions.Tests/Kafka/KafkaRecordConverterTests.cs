// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Kafka.Proto;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Kafka
{
    public class KafkaRecordConverterTests
    {
        [Fact]
        public async Task ConvertAsync_ReturnsSuccess()
        {
            var proto = CreateTestKafkaRecordProto();
            var data = CreateGrpcModelBindingData(proto);

            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            AssertKafkaRecord(output);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsSuccess()
        {
            var proto = CreateTestKafkaRecordProto();
            var bytes = proto.ToByteArray();

            var data1 = new ModelBindingData
            {
                Version = "1.0",
                Source = KafkaRecordConverter.ExpectedBindingSource,
                Content = ByteString.CopyFrom(bytes),
                ContentType = KafkaRecordConverter.ExpectedContentType,
            };
            var data2 = new ModelBindingData
            {
                Version = "1.0",
                Source = KafkaRecordConverter.ExpectedBindingSource,
                Content = ByteString.CopyFrom(bytes),
                ContentType = KafkaRecordConverter.ExpectedContentType,
            };

            var collection = new CollectionModelBindingData();
            collection.ModelBindingData.Add(data1);
            collection.ModelBindingData.Add(data2);

            var context = new TestConverterContext(typeof(KafkaRecord[]), new GrpcCollectionModelBindingData(collection));
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord[];
            Assert.NotNull(output);
            Assert.Equal(2, output.Length);
            AssertKafkaRecord(output[0]);
            AssertKafkaRecord(output[1]);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_WrongSource()
        {
            var proto = CreateTestKafkaRecordProto();

            var data = new GrpcModelBindingData(new ModelBindingData
            {
                Version = "1.0",
                Source = "some-other-source",
                Content = ByteString.CopyFrom(proto.ToByteArray()),
                ContentType = KafkaRecordConverter.ExpectedContentType,
            });

            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(
                "Unexpected binding source 'some-other-source'. Only 'AzureKafkaRecord' is supported.",
                result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_WrongContentType()
        {
            var proto = CreateTestKafkaRecordProto();

            var data = new GrpcModelBindingData(new ModelBindingData
            {
                Version = "1.0",
                Source = KafkaRecordConverter.ExpectedBindingSource,
                Content = ByteString.CopyFrom(proto.ToByteArray()),
                ContentType = "application/json",
            });

            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(
                "Unexpected content-type 'application/json'. Only 'application/x-protobuf' is supported.",
                result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsFailure_WrongSource()
        {
            var proto = CreateTestKafkaRecordProto();

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = "some-other-source",
                Content = ByteString.CopyFrom(proto.ToByteArray()),
                ContentType = KafkaRecordConverter.ExpectedContentType,
            };

            var collection = new CollectionModelBindingData();
            collection.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(KafkaRecord[]), new GrpcCollectionModelBindingData(collection));
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(
                "Unexpected binding source 'some-other-source'. Only 'AzureKafkaRecord' is supported.",
                result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_PreservesNullKeyAndValue()
        {
            var proto = new KafkaRecordProto
            {
                Topic = "test-topic",
                Partition = 0,
                Offset = 100,
                Timestamp = new KafkaTimestampProto
                {
                    UnixTimestampMs = 1700000000000,
                    Type = 1,
                },
            };
            // Key and Value deliberately not set (null semantics via optional bytes)

            var data = CreateGrpcModelBindingData(proto);
            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            Assert.Null(output.Key);
            Assert.Null(output.Value);
            Assert.Equal("test-topic", output.Topic);
        }

        [Fact]
        public async Task ConvertAsync_PreservesHeaders()
        {
            var proto = new KafkaRecordProto
            {
                Topic = "test-topic",
                Partition = 0,
                Offset = 0,
                Value = ByteString.CopyFromUtf8("test"),
                Timestamp = new KafkaTimestampProto { UnixTimestampMs = 0, Type = 0 },
            };
            proto.Headers.Add(new KafkaHeaderProto
            {
                Key = "correlation-id",
                Value = ByteString.CopyFromUtf8("abc-123"),
            });
            proto.Headers.Add(new KafkaHeaderProto
            {
                Key = "null-value-header",
                // Value intentionally not set
            });

            var data = CreateGrpcModelBindingData(proto);
            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            Assert.Equal(2, output.Headers.Length);

            Assert.Equal("correlation-id", output.Headers[0].Key);
            Assert.Equal("abc-123", output.Headers[0].GetValueAsString());

            Assert.Equal("null-value-header", output.Headers[1].Key);
            Assert.Null(output.Headers[1].Value);
        }

        [Fact]
        public async Task ConvertAsync_PreservesLeaderEpoch()
        {
            var proto = CreateTestKafkaRecordProto();
            proto.LeaderEpoch = 42;

            var data = CreateGrpcModelBindingData(proto);
            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            Assert.Equal(42, output.LeaderEpoch);
        }

        [Fact]
        public async Task ConvertAsync_NoLeaderEpoch_ReturnsNull()
        {
            var proto = new KafkaRecordProto
            {
                Topic = "test-topic",
                Partition = 0,
                Offset = 0,
                Value = ByteString.CopyFromUtf8("test"),
                Timestamp = new KafkaTimestampProto { UnixTimestampMs = 0, Type = 0 },
            };
            // LeaderEpoch deliberately not set

            var data = CreateGrpcModelBindingData(proto);
            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            Assert.Null(output.LeaderEpoch);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsUnhandled_NullSource()
        {
            var context = new TestConverterContext(typeof(KafkaRecord), source: null);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }

        [Fact]
        public async Task ConvertAsync_TimestampTypes()
        {
            var proto = new KafkaRecordProto
            {
                Topic = "test-topic",
                Partition = 0,
                Offset = 0,
                Value = ByteString.CopyFromUtf8("test"),
                Timestamp = new KafkaTimestampProto
                {
                    UnixTimestampMs = 1700000000000,
                    Type = 2, // LogAppendTime
                },
            };

            var data = CreateGrpcModelBindingData(proto);
            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            Assert.Equal(KafkaTimestampType.LogAppendTime, output.Timestamp.Type);
            Assert.Equal(1700000000000, output.Timestamp.UnixTimestampMs);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), output.Timestamp.DateTimeOffset);
        }

        [Fact]
        public async Task ConvertAsync_UnknownTimestampType_FallsBackToNotAvailable()
        {
            var proto = new KafkaRecordProto
            {
                Topic = "test-topic",
                Partition = 0,
                Offset = 0,
                Value = ByteString.CopyFromUtf8("test"),
                Timestamp = new KafkaTimestampProto
                {
                    UnixTimestampMs = 1700000000000,
                    Type = 99, // Unknown future value
                },
            };

            var data = CreateGrpcModelBindingData(proto);
            var context = new TestConverterContext(typeof(KafkaRecord), data);
            var converter = new KafkaRecordConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as KafkaRecord;
            Assert.NotNull(output);
            Assert.Equal(KafkaTimestampType.NotAvailable, output.Timestamp.Type);
            Assert.Equal(1700000000000, output.Timestamp.UnixTimestampMs);
        }

        private static KafkaRecordProto CreateTestKafkaRecordProto()
        {
            var proto = new KafkaRecordProto
            {
                Topic = "my-topic",
                Partition = 3,
                Offset = 12345,
                Key = ByteString.CopyFromUtf8("my-key"),
                Value = ByteString.CopyFromUtf8("{\"name\":\"test\"}"),
                Timestamp = new KafkaTimestampProto
                {
                    UnixTimestampMs = 1700000000000,
                    Type = 1, // CreateTime
                },
                LeaderEpoch = 7,
            };
            proto.Headers.Add(new KafkaHeaderProto
            {
                Key = "trace-id",
                Value = ByteString.CopyFromUtf8("trace-abc"),
            });
            return proto;
        }

        private static GrpcModelBindingData CreateGrpcModelBindingData(KafkaRecordProto proto)
        {
            return new GrpcModelBindingData(new ModelBindingData
            {
                Version = "1.0",
                Source = KafkaRecordConverter.ExpectedBindingSource,
                Content = ByteString.CopyFrom(proto.ToByteArray()),
                ContentType = KafkaRecordConverter.ExpectedContentType,
            });
        }

        private static void AssertKafkaRecord(KafkaRecord record)
        {
            Assert.Equal("my-topic", record.Topic);
            Assert.Equal(3, record.Partition);
            Assert.Equal(12345, record.Offset);
            Assert.Equal("my-key", Encoding.UTF8.GetString(record.Key));
            Assert.Equal("{\"name\":\"test\"}", Encoding.UTF8.GetString(record.Value));
            Assert.Equal(1700000000000, record.Timestamp.UnixTimestampMs);
            Assert.Equal(KafkaTimestampType.CreateTime, record.Timestamp.Type);
            Assert.Equal(7, record.LeaderEpoch);
            Assert.Single(record.Headers);
            Assert.Equal("trace-id", record.Headers[0].Key);
            Assert.Equal("trace-abc", record.Headers[0].GetValueAsString());
        }
    }
}
