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
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Blob
{
    public class StringTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public StringTests()
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
        public async Task ConvertAsync_String_WithFilePath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(string), grpcModelBindingData);

            var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(new BinaryData("MyBlobString"));
            var mockResponse = new Mock<Response<BlobDownloadResult>>();
            mockResponse.SetupGet(r => r.Value).Returns(blobDownloadResult);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.DownloadContentAsync()).ReturnsAsync(mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var stringResult = (string)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("MyBlobString", stringResult);
        }

        [Fact]
        public async Task ConvertAsync_String_FilePathWithoutFileExtension_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(string), grpcModelBindingData);

            var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(new BinaryData("MyBlobString"));
            var mockResponse = new Mock<Response<BlobDownloadResult>>();
            mockResponse.SetupGet(r => r.Value).Returns(blobDownloadResult);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.DownloadContentAsync()).ReturnsAsync(mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var stringResult = (string)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("MyBlobString", stringResult);
        }

        [Fact]
        public async Task ConvertAsync_String_WithContainerPath_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(string), grpcModelBindingData);

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
        public async Task ConvertAsync_StringCollection_List_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<string>), grpcModelBindingData);

            var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(new BinaryData("MyBlobString"));
            var mockResponse = new Mock<Response<BlobDownloadResult>>();
            mockResponse.SetupGet(r => r.Value).Returns(blobDownloadResult);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.DownloadContentAsync()).ReturnsAsync(mockResponse.Object);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var stringResult = (IEnumerable<string>)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsAssignableFrom<IEnumerable<string>>(stringResult);
            Assert.Equal("MyBlobString", stringResult.First().ToString());
        }

        [Fact]
        public async Task ConvertAsync_StringCollection_Array_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(string[]), grpcModelBindingData);

            var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(new BinaryData("MyBlobString"));
            var mockResponse = new Mock<Response<BlobDownloadResult>>();
            mockResponse.SetupGet(r => r.Value).Returns(blobDownloadResult);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(m => m.DownloadContentAsync()).ReturnsAsync(mockResponse.Object);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var stringResult = (string[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<string[]>(stringResult);
            Assert.Equal("MyBlobString", stringResult.First().ToString());
        }

        [Fact]
        public async Task ConvertAsync_StringCollection_WithFilePath_ValidContent_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(string[]), grpcModelBindingData);

            var jsonString = JsonConvert.SerializeObject(new List<string> { "1", "2" });
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var stringResult = (string[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<string[]>(stringResult);
            Assert.Equal("1", stringResult.First().ToString());
            Assert.Equal("2", stringResult.Last().ToString());
        }

        [Fact]
        public async Task ConvertAsync_StringCollection_WithFilePath_InvalidContent_Throws_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(string[]), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("[1,2]"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(expectedStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }
    }
}
