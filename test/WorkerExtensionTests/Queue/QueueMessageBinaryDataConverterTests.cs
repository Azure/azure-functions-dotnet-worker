// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests
{
    public class QueueMessageBinaryDataConverterTests
    {
        public QueueMessageBinaryDataConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_BinaryData_ReturnsSuccess()
        {
            var grpcModelBindingData = QueuesTestHelper.GetTestGrpcModelBindingData();
            var context = new TestConverterContext(typeof(BinaryData), grpcModelBindingData);

            var queueMessageConverter = new QueueMessageBinaryDataConverter();
            var conversionResult = await queueMessageConverter.ConvertAsync(context);

            var expectedData = conversionResult.Value as BinaryData;
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("hello world",  expectedData.ToString());
        }

        [Fact]
        public async Task ConvertAsync_ContentSource_AsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(BinaryData), new Object());

            var queueMessageConverter = new QueueMessageBinaryDataConverter();
            var conversionResult = await queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(BinaryData), null);

            var queueMessageConverter = new QueueMessageBinaryDataConverter();
            var conversionResult = await queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotQueueStorageExtension_ReturnsUnhandled()
        {
            var grpcModelBindingData = QueuesTestHelper.GetTestGrpcModelBindingData(source: "anotherExtensions");
            var context = new TestConverterContext(typeof(BinaryData), grpcModelBindingData);

            var queueMessageConverter = new QueueMessageBinaryDataConverter();
            var conversionResult = await queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            var grpcModelBindingData = QueuesTestHelper.GetTestGrpcModelBindingData(contentType: "binary");
            var context = new TestConverterContext(typeof(BinaryData), grpcModelBindingData);

            var queueMessageConverter = new QueueMessageBinaryDataConverter();
            var conversionResult = await queueMessageConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type. Currently only 'application/json' is supported.", conversionResult.Error.Message);
        }
    }
}
