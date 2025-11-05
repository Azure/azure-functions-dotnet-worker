// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Blob
{
    public class StreamTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public StreamTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            var logger = host.Services.GetService<ILogger<BlobStorageConverter>>();

            _mockBlobServiceClient = new Mock<BlobServiceClient>();

            var mockBlobOptions = new Mock<BlobStorageBindingOptions>();
            mockBlobOptions.Object.Client = _mockBlobServiceClient.Object;

            var mockBlobOptionsMonitor = new Mock<IOptionsMonitor<BlobStorageBindingOptions>>();
            mockBlobOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockBlobOptions.Object);

            _blobStorageConverter = new BlobStorageConverter(workerOptions, mockBlobOptionsMonitor.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_Stream_WithFilePath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Stream), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("MyBlobStream"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var streamResult = (Stream)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedStream, streamResult);
        }

        [Fact]
        public async Task ConvertAsync_Stream_FilePathWithoutFileExtension_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Stream), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("MyBlobStream"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var streamResult = (Stream)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedStream, streamResult);
        }

        [Fact]
        public async Task ConvertAsync_Stream_WithContainerPath_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Stream), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();
            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);


            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Equal("'BlobName' cannot be null or empty when binding to a single blob.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_StreamCollection_List_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<Stream>), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("MyBlobStream"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var streamResult = (IEnumerable<Stream>)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsAssignableFrom<IEnumerable<Stream>>(streamResult);
            Assert.Equal(expectedStream.ToString(), streamResult.First().ToString());
        }

        [Fact]
        public async Task ConvertAsync_StreamCollection_Array_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Stream[]), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("MyBlobStream"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var streamResult = (Stream[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedStream.ToString(), streamResult.First().ToString());
        }

        [Fact]
        public async Task ConvertAsync_StreamCollection_WithSubdirectoryPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "test"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Stream[]), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("MyBlobStream"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var streamResult = (Stream[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedStream.ToString(), streamResult.First().ToString());
        }

        [Fact]
        public async Task ConvertAsync_StreamCollection_WithFilePath_Throws_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "test.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Stream[]), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("[{\"name\": \"Stream1\"}]"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<NotSupportedException>(conversionResult.Error);
            Assert.Contains("Deserialization of types without a parameterless constructor, a singular parameterized constructor, or a parameterized constructor annotated with 'JsonConstructorAttribute' is not supported.", conversionResult.Error.Message);
        }
    }
}
