// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
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
    public class BlobStorageConverterCoreTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public BlobStorageConverterCoreTests()
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
        public async Task ConvertAsync_ContentSource_AsObject_ReturnsUnhandled()
        {
            // Arrange
            var context = new TestConverterContext(typeof(BlobClient), new object());

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            // Arrange
            var context = new TestConverterContext(typeof(BlobClient), null);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ResultIsEmpty_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns((BlobClient)null);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unable to convert blob binding data to type 'BlobClient'.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ThrowsException_ReturnsFailure()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            _mockBlobServiceClient
                .Setup(m => m.GetBlobContainerClient(It.IsAny<string>()))
                .Throws(new Exception());

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotBlobExtension_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "anotherExtensions");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected binding source 'anotherExtensions'. Only 'AzureStorageBlobs' is supported.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs", contentType: "binary");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type 'binary'. Only 'application/json' is supported.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_InvalidContent_Throws_ReturnsFailed()
        {
            // Arrange
            string badJsonData = $@"{{
                                ""Connection"" : ""Connection"",
                                ""ContainerName"" ""ContainerName"",
                                ""BlobName"" : ""BlobName"",
                            }}";

            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(new BinaryData(badJsonData), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_MissingConnectionParameter_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(null), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("'Connection' cannot be null or empty.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_MissingContainerNameParameter_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(container: null), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobClient), grpcModelBindingData);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobClient)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("'ContainerName' cannot be null or empty.", conversionResult.Error.Message);
        }
    }
}
