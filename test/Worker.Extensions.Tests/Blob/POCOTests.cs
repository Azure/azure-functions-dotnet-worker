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
    public class POCOTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public POCOTests()
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
        public async Task ConvertAsync_POCO_WithFilePath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book), grpcModelBindingData);

            var expectedBook = new Book() { Name = "MyBook" };

            var testStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name\":\"MyBook\"}"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(testStream);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var pocoResult = (Book)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedBook.GetType(), pocoResult.GetType());
            Assert.Equal(expectedBook.Name, pocoResult.Name);
        }

        [Fact]
        public async Task ConvertAsync_POCO_FilePathWithoutFileExtension_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book), grpcModelBindingData);

            var expectedBook = new Book() { Name = "MyBook" };

            var testStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name\":\"MyBook\"}"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(testStream);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var pocoResult = (Book)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedBook.GetType(), pocoResult.GetType());
            Assert.Equal(expectedBook.Name, pocoResult.Name);
        }

        [Fact]
        public async Task ConvertAsync_POCO_WithContainerPath_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book), grpcModelBindingData);

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
        public async Task ConvertAsync_POCO_InvalidJson_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book), grpcModelBindingData);

            var expectedBook = new Book() { Name = "MyBook" };

            var testStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name:\"MyBook\"}"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(testStream);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var pocoResult = (Book)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_POCOCollection_List_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<Book>), grpcModelBindingData);

            var expectedBookList = new List<Book>() { new Book() { Name = "MyBook" } };
            var testStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name\":\"MyBook\"}"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(testStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var pocoResult = (IEnumerable<Book>)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsAssignableFrom<IEnumerable<Book>>(pocoResult);
            Assert.Equal(expectedBookList[0].Name, pocoResult.First().Name);
        }

        [Fact]
        public async Task ConvertAsync_POCOCollection_Array_WithContainerPath_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book[]), grpcModelBindingData);

            var expectedBookList = new Book[] { new Book() { Name = "MyBook" } };
            var testStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name\":\"MyBook\"}"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(testStream);

            var mockBlobItemResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockBlobItemResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var pocoResult = (Book[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<Book[]>(pocoResult);
            Assert.Equal(expectedBookList[0].Name, pocoResult.First().Name);
        }

        [Fact]
        public async Task ConvertAsync_POCOCollection_InvalidJson_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book[]), grpcModelBindingData);

            var expectedBookList = new Book[] { new Book() { Name = "MyBook" } };
            var testStream = new MemoryStream(Encoding.UTF8.GetBytes("i should fail"));
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.OpenReadAsync(0, default, default, default))
                .ReturnsAsync(testStream);

            var blobItemMockResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem> { BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, blobItemMockResponse.Object);

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

        [Fact]
        public async Task ConvertAsync_POCOCollection_WithFilePath_ValidContent_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book[]), grpcModelBindingData);

            var jsonString = JsonConvert.SerializeObject(new List<object> { new { Name = "MyBook" }, new { Name = "MySecondBook" }});
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
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
            var bookResult = (Book[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<Book[]>(bookResult);
            Assert.Equal("MyBook", bookResult[0].Name);
            Assert.Equal("MySecondBook", bookResult[1].Name);
        }

        [Fact]
        public async Task ConvertAsync_POCOCollection_WithFilePath_InvalidContent_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(Book[]), grpcModelBindingData);

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("[1,2]"));
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
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        public class Book
        {
            public string Name { get; set; }
        }
    }
}
