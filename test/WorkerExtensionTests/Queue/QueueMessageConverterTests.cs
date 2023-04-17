// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests
{
    public class QueueMessageConverterTests
    {
        private QueueMessageConverter _queueMessageConverter;

        public QueueMessageConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<QueueMessageConverter>>();

            _queueMessageConverter = new QueueMessageConverter(logger);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_QueueMessage_ReturnsSuccess()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(QueueMessage), grpcModelBindingData);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            var expectedData = (QueueMessage)conversionResult.Value;
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("hello world", expectedData.Body.ToString());
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_BinaryData_ReturnsSuccess()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(BinaryData), grpcModelBindingData);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            var expectedData = (BinaryData)conversionResult.Value;

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("hello world",  expectedData.ToString());
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_JObject_ReturnsSuccess()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(message: "{\\\"text\\\":\\\"hello world\\\"}"));
            var context = new TestConverterContext(typeof(JObject), grpcModelBindingData);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            var expectedData = (JObject)conversionResult.Value;
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("hello world", (string) expectedData["text"]);
        }

        [Fact]
        public async Task ConvertAsync_InvalidJsonMessage_JObject_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(message: "{\"text\\\":\\\"hello world\\\"}"));
            var context = new TestConverterContext(typeof(JObject), grpcModelBindingData);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal(typeof(InvalidOperationException), conversionResult.Error.GetType());
            Assert.Contains("Binding parameters to complex objects uses Json.NET serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ContentSource_AsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(QueueMessage), new Object());

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(QueueMessage), null);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotQueueStorageExtension_ReturnsUnhandled()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(), source: "anotherExtensions");
            var context = new TestConverterContext(typeof(QueueMessage), grpcModelBindingData);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(), contentType: "binary");
            var context = new TestConverterContext(typeof(QueueMessage), grpcModelBindingData);

            var conversionResult = await _queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type. Currently only 'application/json' is supported.", conversionResult.Error.Message);
        }

        private BinaryData GetTestBinaryData(string messageId = "fbb84c41-9f1f-4c75-950c-72d0541fb8ae", string message = "hello world")
        {
            string jsonData = $@"{{
                                ""MessageId"" : ""{messageId}"",
                                ""PopReceipt"" : ""AgAAAAMAAAAAAAAASm\u002B7xBZv2QE="",
                                ""MessageText"" : ""{message}"",
                                ""Body"" : {{}},
                                ""NextVisibleOn"" : ""2023-04-14T21:19:16+00:00"",
                                ""InsertedOn"" : ""2023-04-14T21:09:14+00:00"",
                                ""ExpiresOn"" : ""2023-04-21T21:09:14+00:00"",
                                ""DequeueCount"" : 1
                            }}";

            return new BinaryData(jsonData);
        }

        private GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData content, string source = "AzureStorageQueues", string contentType = "application/json")
        {
            var data = new ModelBindingData()
            {
                Version = "1.0",
                Source = source,
                Content = ByteString.CopyFrom(content),
                ContentType = contentType
            };

            return new GrpcModelBindingData(data);
        }
    }
}
