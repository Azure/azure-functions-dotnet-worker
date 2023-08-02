// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventHubs;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.EventHubs
{
    public class EventDataConverterTests
    {
        [Fact]
        public async Task ConvertAsync_ReturnsSuccess()
        {
            var eventData = CreateEventData();

            var data = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureEventHubsEventData",
                Content = ByteString.CopyFrom(ConvertEventDataToBinaryData(eventData)),
                ContentType = Constants.BinaryContentType
            });

            var context = new TestConverterContext(typeof(string), data);
            var converter = new EventDataConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as EventData;
            Assert.NotNull(output);
            AssertEventData(output);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsSuccess()
        {
            var message = CreateEventData();

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = "AzureEventHubsEventData",
                Content = ByteString.CopyFrom(ConvertEventDataToBinaryData(message)),
                ContentType = Constants.BinaryContentType
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(data);
            array.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(string), new GrpcCollectionModelBindingData(array));
            var converter = new EventDataConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as EventData[];
            Assert.NotNull(output);
            Assert.Equal(2, output.Length);
            AssertEventData(output[0]);
            AssertEventData(output[1]);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_WrongContentType()
        {
            var eventData = CreateEventData();

            var data = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = Constants.BindingSource,
                Content = ByteString.CopyFrom(ConvertEventDataToBinaryData(eventData)),
                ContentType = "application/json"
            });

            var context = new TestConverterContext(typeof(string), data);
            var converter = new EventDataConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as EventData;
            Assert.Null(output);
            Assert.Equal("Unexpected content-type 'application/json'. Only 'application/octet-stream' is supported.", result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsFailure_WrongContentType()
        {
            var message = CreateEventData();

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = Constants.BindingSource,
                Content = ByteString.CopyFrom(ConvertEventDataToBinaryData(message)),
                ContentType = "application/json"
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(data);
            array.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(string), new GrpcCollectionModelBindingData(array));
            var converter = new EventDataConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as EventData[];
            Assert.Null(output);
            Assert.Equal("Unexpected content-type 'application/json'. Only 'application/octet-stream' is supported.", result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_WrongSource()
        {
            var eventData = CreateEventData();

            var data = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = "some-other-source",
                Content = ByteString.CopyFrom(ConvertEventDataToBinaryData(eventData)),
                ContentType = Constants.BinaryContentType
            });

            var context = new TestConverterContext(typeof(string), data);
            var converter = new EventDataConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as EventData;
            Assert.Null(output);
            Assert.Equal("Unexpected binding source 'some-other-source'. Only 'AzureEventHubsEventData' is supported.", result.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ReturnsFailure_WrongSource()
        {
            var message = CreateEventData();

            var data = new ModelBindingData
            {
                Version = "1.0",
                Source = "some-other-source",
                Content = ByteString.CopyFrom(ConvertEventDataToBinaryData(message)),
                ContentType = Constants.BinaryContentType
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(data);
            array.ModelBindingData.Add(data);

            var context = new TestConverterContext(typeof(string), new GrpcCollectionModelBindingData(array));
            var converter = new EventDataConverter();
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
            var output = result.Value as EventData[];
            Assert.Null(output);
            Assert.Equal("Unexpected binding source 'some-other-source'. Only 'AzureEventHubsEventData' is supported.", result.Error.Message);
        }

        private static void AssertEventData(EventData output)
        {
            Assert.Equal("body", output.EventBody.ToString());
            Assert.Equal("messageId", output.MessageId);
            Assert.Equal("correlationId", output.CorrelationId);
            Assert.Equal("contentType", output.ContentType);
        }

        private static EventData CreateEventData()
        {
            return new EventData("body")
            {
                ContentType = "contentType",
                CorrelationId = "correlationId",
                MessageId = "messageId",
            };
        }

        private static BinaryData ConvertEventDataToBinaryData(EventData @event)
        {
            return @event.GetRawAmqpMessage().ToBytes();
        }
    }
}