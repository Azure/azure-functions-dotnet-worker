// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

// Scenarios for BlobBaseClient, BlockBlobClient, PageBlobClient, and AppendBlobClient
// are tested via E2E tests as Moq does not support mocking extension methods directly
namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Blob
{
    public class BlobClientTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public BlobClientTests()
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
        public async Task ConvertAsync_BlobClient_WithFilePath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.Name).Returns("MyBlob");

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("MyBlob", clientResult.Name);
        }

        [Fact]
        public async Task ConvertAsync_BlobClient_FilePathWithoutFileExtension_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "test"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.Name).Returns("test");

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("test", clientResult.Name);
        }

        [Fact]
        public async Task ConvertAsync_BlobClient_WithContainerPath_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("'BlobName' cannot be null or empty when binding to a single blob.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_BlobClientCollection_List_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<BlobClient>), grpcModelBindingData);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.Name).Returns("MyBlob");

            var mockResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (IEnumerable<BlobClient>)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsAssignableFrom<IEnumerable<BlobClient>>(clientResult);
            Assert.Equal("MyBlob", clientResult.First().Name);
        }

        [Fact]
        public async Task ConvertAsync_BlobClientCollection_Array_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient[]), grpcModelBindingData);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.Name).Returns("MyBlob");

            var mockResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<BlobClient[]>(clientResult);
            Assert.Equal("MyBlob", clientResult.First().Name);
        }

        [Fact]
        public async Task ConvertAsync_BlobClientCollection_WithSubdirectoryPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData("test"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient[]), grpcModelBindingData);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.Name).Returns("MyBlob");

            var mockResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<BlobClient[]>(clientResult);
            Assert.Equal("MyBlob", clientResult.First().Name);
        }

        [Fact]
        public async Task ConvertAsync_BlobClientCollection_WithFilePath_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<BlobClient>), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();
            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Contains("Binding to a blob client collection with a blob path is not supported.", conversionResult.Error.Message);
        }
    }
}
