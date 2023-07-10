// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    public class ByteArrayTests
    {
        private readonly BlobStorageConverter _blobStorageConverter;
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

        public ByteArrayTests()
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
        public async Task ConvertAsync_ValidModelBindingData_ByteArray_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(byte[]), grpcModelBindingData);

            var expectedByteArray = Encoding.UTF8.GetBytes("MyBlobByteArray");
            var testStream = new MemoryStream(expectedByteArray)
            {
                Position = 0
            };

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.DownloadToAsync(It.IsAny<Stream>(), default))
                .Callback<Stream, CancellationToken>(async (s, _) => await testStream.CopyToAsync(s))
                .ReturnsAsync(new Mock<Response>().Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var byteResult = (byte[])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<byte[]>(byteResult);
            Assert.Equal(new byte[0], byteResult);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_ByteArrayCollection_List_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(IEnumerable<byte[]>), grpcModelBindingData);

            var expectedByteArray = Encoding.UTF8.GetBytes("MyBlobByteArray");
            var testStream = new MemoryStream(expectedByteArray);
            testStream.Position = 0;

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.DownloadToAsync(It.IsAny<Stream>(), default))
                .Callback<Stream, CancellationToken>(async (s, _) => await testStream.CopyToAsync(s))
                .ReturnsAsync(new Mock<Response>().Object);

            var mockResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var byteResult = (IEnumerable<byte[]>)conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsAssignableFrom<IEnumerable<byte[]>>(byteResult);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_ByteArrayCollection_Array_ReturnsSuccess()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(byte[][]), grpcModelBindingData);

            var expectedByteArray = Encoding.UTF8.GetBytes("MyBlobByteArray");
            var testStream = new MemoryStream(expectedByteArray);
            testStream.Position = 0;

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(m => m.DownloadToAsync(It.IsAny<Stream>(), default))
                .Callback<Stream, CancellationToken>(async (s, _) => await testStream.CopyToAsync(s))
                .ReturnsAsync(new Mock<Response>().Object);

            var mockResponse = new Mock<Response>();
            var expectedOutput = Page<BlobItem>.FromValues(new List<BlobItem>{ BlobsModelFactory.BlobItem("MyBlob") }, continuationToken: null, mockResponse.Object);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(m => m.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainer.Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
                            .Returns(AsyncPageable<BlobItem>.FromPages(new List<Page<BlobItem>> { expectedOutput }));

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);
            var byteResult = (byte[][])conversionResult.Value;

            // Assert
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.IsType<byte[][]>(byteResult);
        }

        [Fact]
        public async Task ConvertAsync_ByteArray_SingleBindingWithoutBlobName_ReturnsFailed()
        {
            // Arrange
            var grpcModelBindingData = Helper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(), "AzureStorageBlobs");
            var context = new TestConverterContext(typeof(byte[]), grpcModelBindingData);

            var mockContainer = new Mock<BlobContainerClient>();

            _mockBlobServiceClient.Setup(m => m.GetBlobContainerClient(It.IsAny<string>())).Returns(mockContainer.Object);

            // Act
            var conversionResult = await _blobStorageConverter.ConvertAsync(context);

            // Assert
            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.IsType<InvalidOperationException>(conversionResult.Error);
            Assert.Equal("'BlobName' cannot be null or empty when binding to a single blob.", conversionResult.Error.Message);
        }
    }
}
