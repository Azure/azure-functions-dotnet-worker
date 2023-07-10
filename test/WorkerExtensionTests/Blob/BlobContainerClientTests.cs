// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests.Blob
{
    public class BlobContainerClientTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public BlobContainerClientTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            var logger = host.Services.GetService<ILogger<BlobStorageConverter>>();

            _mockBlobServiceClient = new Mock<BlobServiceClient>();

            var mockBlobOptions = new Mock<BlobStorageBindingOptions>();
            mockBlobOptions.Object.Client = _mockBlobServiceClient.Object;

            var mockBlobOptionsSnapshot = new Mock<IOptionsSnapshot<BlobStorageBindingOptions>>();
            mockBlobOptionsSnapshot
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockBlobOptions.Object);

            _blobStorageConverter = new BlobStorageConverter(workerOptions, mockBlobOptionsSnapshot.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_BlobContainerClient_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobContainerClient), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.Name).Returns("MyContainer");

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var clientResult = (BlobContainerClient)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("MyContainer", clientResult.Name);
        }

        [Fact]
        public async Task ConvertAsync_BlobContainerClient_BindingWithBlobName_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(BlobContainerClient), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.Name).Returns("MyContainer");

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Equal("Binding to a BlobContainerClient with a blob path is not supported. Either bind to the container path, or use BlobClient instead.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_BlobContainerClient_CollectionBinding_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<BlobContainerClient>), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.Name).Returns("MyContainer");

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Equal("Binding to a BlobContainerClient collection is not supported.", conversionResult.Error.Message);
        }
    }
}
