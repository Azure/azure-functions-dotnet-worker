// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Blob
{
    /// <summary>
    /// Routing-level tests for the individual converters created by splitting <c>BlobStorageConverter</c>.
    /// These assert that each converter only claims its own target type(s) and otherwise returns
    /// <see cref="ConversionStatus.Unhandled"/> so the next converter in the registration order gets a turn.
    /// </summary>
    public class BlobConverterRoutingTests
    {
        private readonly IOptions<WorkerOptions> _workerOptions;
        private readonly IOptionsMonitor<BlobStorageBindingOptions> _blobOptionsMonitor;
        private readonly ILoggerFactory _loggerFactory;

        public BlobConverterRoutingTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();

            _workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            _loggerFactory = host.Services.GetService<ILoggerFactory>();

            var mockBlobOptions = new Mock<BlobStorageBindingOptions>();
            mockBlobOptions.Object.Client = new Mock<BlobServiceClient>().Object;

            var mockBlobOptionsMonitor = new Mock<IOptionsMonitor<BlobStorageBindingOptions>>();
            mockBlobOptionsMonitor.Setup(m => m.Get(It.IsAny<string>())).Returns(mockBlobOptions.Object);
            _blobOptionsMonitor = mockBlobOptionsMonitor.Object;
        }

        private static ConverterContext Context(System.Type targetType)
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(BlobTestHelper.GetTestBinaryData(blobName: "MyBlob.txt"), "AzureStorageBlobs");
            return new TestConverterContext(targetType, grpcModelBindingData);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(Stream))]
        [InlineData(typeof(BlobClient))]
        [InlineData(typeof(string[]))]
        public async Task BlobStringConverter_ReturnsUnhandled_ForNonStringTarget(System.Type targetType)
        {
            var converter = new BlobStringConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobStringConverter>());

            var result = await converter.ConvertAsync(Context(targetType));

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(byte[][]))]
        [InlineData(typeof(BlobClient))]
        public async Task BlobByteArrayConverter_ReturnsUnhandled_ForNonByteArrayTarget(System.Type targetType)
        {
            var converter = new BlobByteArrayConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobByteArrayConverter>());

            var result = await converter.ConvertAsync(Context(targetType));

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(BlobContainerClient))]
        [InlineData(typeof(IEnumerable<BlobClient>))]
        public async Task BlobClientConverter_ReturnsUnhandled_ForNonClientTarget(System.Type targetType)
        {
            var converter = new BlobClientConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobClientConverter>());

            var result = await converter.ConvertAsync(Context(targetType));

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }

        [Fact]
        public async Task BlobContainerClientConverter_ReturnsUnhandled_ForNonContainerTarget()
        {
            var converter = new BlobContainerClientConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobContainerClientConverter>());

            var result = await converter.ConvertAsync(Context(typeof(BlobClient)));

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }

        // byte[] and string must be treated as scalars, never as collections, so the collection converter
        // must defer them. (string and byte[] are special-cased in TypeExtensions.IsCollectionType.)
        [Theory]
        [InlineData(typeof(byte[]))]
        [InlineData(typeof(string))]
        [InlineData(typeof(BlobClient))]
        public async Task BlobCollectionConverter_ReturnsUnhandled_ForScalarTarget(System.Type targetType)
        {
            var converter = new BlobCollectionConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobCollectionConverter>());

            var result = await converter.ConvertAsync(Context(targetType));

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }

        [Theory]
        [InlineData(typeof(BlobClient))]
        [InlineData(typeof(string))]
        public async Task Converters_ReturnUnhandled_ForNonModelBindingDataSource(System.Type targetType)
        {
            var context = new TestConverterContext(targetType, new object());

            var stringResult = await new BlobStringConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobStringConverter>()).ConvertAsync(context);
            var pocoResult = await new BlobPocoConverter(_workerOptions, _blobOptionsMonitor, _loggerFactory.CreateLogger<BlobPocoConverter>()).ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, stringResult.Status);
            Assert.Equal(ConversionStatus.Unhandled, pocoResult.Status);
        }
    }
}
