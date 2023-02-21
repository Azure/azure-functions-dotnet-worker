// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests
{
    public class BlobStorageConverterTests
    {
        private Mock<BlobStorageConverter> mock = new(MockBehavior.Strict);

        public BlobStorageConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => {}).Build();
            var _workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            var _blobOptions = host.Services.GetService<IOptionsSnapshot<BlobStorageBindingOptions>>();
            var logger = host.Services.GetService<ILogger<BlobStorageConverter>>();

            //_converter = new BlobStorageConverter(_workerOptions, _blobOptions, logger);
            mock = new Mock<BlobStorageConverter>(_workerOptions, _blobOptions, logger);
            mock.CallBase = true;
        }

        [Fact]
        public async Task ConvertAsync_SourceAsObject_Unhandled()
        {
            var context = new TestConverterContext(typeof(string), new Object());

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_Works()
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            object source = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var context = new TestConverterContext(typeof(string), source);
            mock.Setup(c => c.ToTargetTypeAsync(typeof(string),"connection", "container", "BlobName")).ReturnsAsync("abc");

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_Fails()
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            object source = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var context = new TestConverterContext(typeof(string), source);
            mock.Setup(c => c.ToTargetTypeAsync(typeof(string), "connection", "container", "BlobName")).ThrowsAsync(new Exception());

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Theory]
        [InlineData(Constants.BlobExtensionName, true)]
        [InlineData(" ", false)]
        [InlineData("incorrect.value", false)]
        public void IsBlobExtension_GrpcModelBindingData_Works(string sourceVal, bool expectedResult)
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = sourceVal,
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var result = mock.Object.IsBlobExtension(grpcModelBindingData);

            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void GetBindingDataContent_GrpcModelBindingData_Works()
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var result = mock.Object.GetBindingDataContent(grpcModelBindingData);

            Assert.Equal(3, result.Count);

            result.TryGetValue(Constants.Connection, out var connectionName);
            result.TryGetValue(Constants.ContainerName, out var containerName);
            result.TryGetValue(Constants.BlobName, out var blobName);

            Assert.Equal("connection", connectionName);
            Assert.Equal("container", containerName);
            Assert.Equal("BlobName", blobName);
        }

        [Fact]
        public async Task GetBindingDataContent_GrpcModelBindingData_Throws()
        {
            var binaryData = new BinaryData("{" +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var dict = mock.Object.GetBindingDataContent(grpcModelBindingData);

            try
            {
                var result = await mock.Object.ConvertModelBindingDataAsync(dict, typeof(string), grpcModelBindingData);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
            }

            /*
                        Assert.Equal(3, result.Count);

                        result.TryGetValue(Constants.Connection, out var connectionName);
                        result.TryGetValue(Constants.ContainerName, out var containerName);
                        result.TryGetValue(Constants.BlobName, out var blobName);

                        Assert.Equal("connection", connectionName);
                        Assert.Equal("container", containerName);
                        Assert.Equal("BlobName", blobName);*/
        }
    }
}
