// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests
{
    public class ServiceBusReceivedMessageConverterTests
    {
        [Fact]
        public async Task ConvertAsync_ReturnsSuccess()
        {
            var lockToken = Guid.NewGuid();
            var message = CreateReceivedMessage(lockToken);

            var data = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureServiceBusReceivedMessage",
                Content = ByteString.CopyFrom(ConvertReceivedMessageToBinaryData(message)),
                ContentType = Constants.BinaryContentType
            });
            var context = new TestConverterContext(typeof(string), data);
            var converter = new ServiceBusReceivedMessageConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as ServiceBusReceivedMessage;
            Assert.NotNull(output);
            AssertReceivedMessage(output, lockToken);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsSuccess()
        {
            var lockToken = Guid.NewGuid();
            var message = CreateReceivedMessage(lockToken);

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = "AzureServiceBusReceivedMessage",
                Content = ByteString.CopyFrom(ConvertReceivedMessageToBinaryData(message)),
                ContentType = Constants.BinaryContentType
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(data);
            array.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(string), new GrpcCollectionModelBindingData(array));
            var converter = new ServiceBusReceivedMessageConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as ServiceBusReceivedMessage[];
            Assert.NotNull(output);
            Assert.Equal(2, output.Length);
            AssertReceivedMessage(output[0], lockToken);
            AssertReceivedMessage(output[1], lockToken);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_WrongContentType()
        {
            var lockToken = Guid.NewGuid();
            var message = CreateReceivedMessage(lockToken);

            var data = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = Constants.BindingSource,
                Content = ByteString.CopyFrom(ConvertReceivedMessageToBinaryData(message)),
                ContentType = "application/json"
            });
            var context = new TestConverterContext(typeof(string), data);
            var converter = new ServiceBusReceivedMessageConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as ServiceBusReceivedMessage;
            Assert.Null(output);
            Assert.Equal("Unexpected content-type 'application/json'. Only 'application/octet-stream' is supported.", result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsFailure_WrongContentType()
        {
            var lockToken = Guid.NewGuid();
            var message = CreateReceivedMessage(lockToken);

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = Constants.BindingSource,
                Content = ByteString.CopyFrom(ConvertReceivedMessageToBinaryData(message)),
                ContentType = "application/json"
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(data);
            array.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(string), new GrpcCollectionModelBindingData(array));
            var converter = new ServiceBusReceivedMessageConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as ServiceBusReceivedMessage[];
            Assert.Null(output);
            Assert.Equal("Unexpected content-type 'application/json'. Only 'application/octet-stream' is supported.", result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_WrongSource()
        {
            var lockToken = Guid.NewGuid();
            var message = CreateReceivedMessage(lockToken);

            var data = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = "some-other-source",
                Content = ByteString.CopyFrom(ConvertReceivedMessageToBinaryData(message)),
                ContentType = Constants.BinaryContentType
            });
            var context = new TestConverterContext(typeof(string), data);
            var converter = new ServiceBusReceivedMessageConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as ServiceBusReceivedMessage;
            Assert.Null(output);
            Assert.Equal("Unexpected binding source 'some-other-source'. Only 'AzureServiceBusReceivedMessage' is supported.", result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsFailure_WrongSource()
        {
            var lockToken = Guid.NewGuid();
            var message = CreateReceivedMessage(lockToken);

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = "some-other-source",
                Content = ByteString.CopyFrom(ConvertReceivedMessageToBinaryData(message)),
                ContentType = Constants.BinaryContentType
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(data);
            array.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(string), new GrpcCollectionModelBindingData(array));
            var converter = new ServiceBusReceivedMessageConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as ServiceBusReceivedMessage[];
            Assert.Null(output);
            Assert.Equal("Unexpected binding source 'some-other-source'. Only 'AzureServiceBusReceivedMessage' is supported.", result.Error.Message);
        }

        private static void AssertReceivedMessage(ServiceBusReceivedMessage output, Guid lockToken)
        {
            Assert.Equal("body", output.Body.ToString());
            Assert.Equal("messageId", output.MessageId);
            Assert.Equal("correlationId", output.CorrelationId);
            Assert.Equal("sessionId", output.SessionId);
            Assert.Equal("replyTo", output.ReplyTo);
            Assert.Equal("replyToSessionId", output.ReplyToSessionId);
            Assert.Equal("contentType", output.ContentType);
            Assert.Equal("label", output.Subject);
            Assert.Equal("to", output.To);
            Assert.Equal("partitionKey", output.PartitionKey);
            Assert.Equal("viaPartitionKey", output.TransactionPartitionKey);
            Assert.Equal("deadLetterSource", output.DeadLetterSource);
            Assert.Equal(1, output.EnqueuedSequenceNumber);
            Assert.Equal(lockToken.ToString(), output.LockToken);
        }

        private static ServiceBusReceivedMessage CreateReceivedMessage(Guid lockToken)
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString("body"),
                messageId: "messageId",
                correlationId: "correlationId",
                sessionId: "sessionId",
                replyTo: "replyTo",
                replyToSessionId: "replyToSessionId",
                contentType: "contentType",
                subject: "label",
                to: "to",
                partitionKey: "partitionKey",
                viaPartitionKey: "viaPartitionKey",
                deadLetterSource: "deadLetterSource",
                enqueuedSequenceNumber: 1,
                lockTokenGuid: lockToken);
        }

        private static BinaryData ConvertReceivedMessageToBinaryData(ServiceBusReceivedMessage message)
        {
            ReadOnlyMemory<byte> messageBytes = message.GetRawAmqpMessage().ToBytes().ToMemory();

            byte[] lockTokenBytes = Guid.Parse(message.LockToken).ToByteArray();

            // The lock token is a 16 byte GUID
            const int lockTokenLength = 16;

            byte[] combinedBytes = new byte[messageBytes.Length + lockTokenLength];

            // The 16 lock token bytes go in the beginning
            lockTokenBytes.CopyTo(combinedBytes.AsSpan());

            // The AMQP message bytes go after the lock token bytes
            messageBytes.CopyTo(combinedBytes.AsMemory(lockTokenLength));

            return new BinaryData(combinedBytes);
        }
    }
}