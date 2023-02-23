// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
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
        private Mock<BlobStorageConverter> _mockBlobStorageConverter;

        public BlobStorageConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            var blobOptions = host.Services.GetService<IOptionsSnapshot<BlobStorageBindingOptions>>();
            var logger = host.Services.GetService<ILogger<BlobStorageConverter>>();

            _mockBlobStorageConverter = new Mock<BlobStorageConverter>(workerOptions, blobOptions, logger);
            _mockBlobStorageConverter.CallBase = true;
        }

        [Fact]
        public async Task ConvertAsync_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(string), new Object());

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_ReturnsSuccess()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            _mockBlobStorageConverter
                .Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName))
                .ReturnsAsync("test");

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsCollectionModelBindingData_ReturnsSuccess()
        {
            object source = GetTestGrpcCollectionModelBindingData();
            var context = new TestConverterContext(typeof(string), source);
            _mockBlobStorageConverter
                .Setup(c => c.ConvertFromCollectionBindingDataAsync(context, (Worker.Core.CollectionModelBindingData) source))
                .Returns(new ValueTask<ConversionResult>(ConversionResult.Success("test")));

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_ReturnsFailure()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            _mockBlobStorageConverter
                .Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName))
                .ThrowsAsync(new Exception());

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertFromBindingDataAsync_ReturnsSuccess()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var contentDict = GetTestContentDict();
            var context = new TestConverterContext(typeof(string), source);
            var modelBindingData = (Worker.Core.ModelBindingData) source;

            _mockBlobStorageConverter
                .Setup(c => c.ConvertModelBindingDataAsync(contentDict, typeof(string), modelBindingData))
                .ReturnsAsync(new ValueTask<ConversionResult>(ConversionResult.Success("test")));

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertFromBindingDataAsync(context, modelBindingData);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertFromBindingDataAsync_ReturnsFailure()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var contentDict = GetTestContentDict();
            var context = new TestConverterContext(typeof(string), source);

            _mockBlobStorageConverter
                .Setup(c => c.ConvertModelBindingDataAsync(contentDict, typeof(string), (Worker.Core.ModelBindingData)source))
                .ThrowsAsync(new Exception());

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertFromBindingDataAsync(context, (Worker.Core.ModelBindingData)source);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertFromBindingDataAsync_ReturnsUnhandled()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            var dict = _mockBlobStorageConverter.Object.GetBindingDataContent((Worker.Core.ModelBindingData)source);

            _mockBlobStorageConverter.Setup(c => c.ConvertModelBindingDataAsync(dict, typeof(string), (Worker.Core.ModelBindingData)source)).ReturnsAsync(null);

            var conversionResult = await _mockBlobStorageConverter.Object.ConvertFromBindingDataAsync(context, (Worker.Core.ModelBindingData)source);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Theory]
        [InlineData(Constants.BlobExtensionName, true)]
        [InlineData(" ", false)]
        [InlineData("incorrect-value", false)]
        public void IsBlobExtension_MatchesExpectedOutput(string sourceVal, bool expectedResult)
        {
            var grpcModelBindingData = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = sourceVal,
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "application/json"
            });

            var result = _mockBlobStorageConverter.Object.IsBlobExtension(grpcModelBindingData);

            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void GetBindingDataContent_CompleteGrpcModelBindingData_Works()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var result = _mockBlobStorageConverter.Object.GetBindingDataContent(grpcModelBindingData);

            result.TryGetValue(Constants.Connection, out var connectionName);
            result.TryGetValue(Constants.ContainerName, out var containerName);
            result.TryGetValue(Constants.BlobName, out var blobName);

            Assert.Equal(3, result.Count);
            Assert.Equal(Constants.Connection, connectionName);
            Assert.Equal(Constants.ContainerName, containerName);
            Assert.Equal(Constants.BlobName, blobName);
        }

        [Fact]
        public void GetBindingDataContent_IncompleteGrpcModelBindingData_ReturnsNull()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());

            var result = _mockBlobStorageConverter.Object.GetBindingDataContent(grpcModelBindingData);

            result.TryGetValue(Constants.Connection, out var connectionName);
            result.TryGetValue(Constants.ContainerName, out var containerName);
            result.TryGetValue(Constants.BlobName, out var blobName);

            Assert.Single(result);
            Assert.True(connectionName is null);
            Assert.True(containerName is null);
            Assert.Equal(Constants.BlobName, blobName);
        }

        [Fact]
        public void GetBindingDataContent_UnSupportedContentType_Throws()
        {
            var grpcModelBindingData = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = Constants.BlobExtensionName,
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "NotSupported"
            });

            try
            {
                var dict = _mockBlobStorageConverter.Object.GetBindingDataContent(grpcModelBindingData);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
            }
        }

        [Fact]
        public async Task ConvertModelBindingDataAsync_IncompleteGrpcModelBindingData_Throws()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var dict = _mockBlobStorageConverter.Object.GetBindingDataContent(grpcModelBindingData);
            _mockBlobStorageConverter.Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync("test");

            try
            {
                var result = await _mockBlobStorageConverter.Object.ConvertModelBindingDataAsync(dict, typeof(string), grpcModelBindingData);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (ArgumentNullException) { }
        }

        [Fact]
        public async Task ConvertModelBindingDataAsync_GrpcModelBindingData_Works()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var contentDict = GetTestContentDict();

            _mockBlobStorageConverter
                .Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName))
                .ReturnsAsync("test");

            var result = await _mockBlobStorageConverter.Object.ConvertModelBindingDataAsync(contentDict, typeof(string), grpcModelBindingData);

            Assert.Equal(typeof(string), result.GetType());

        }

        [Fact]
        public async Task ToTargetTypeAsync_Works()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            byte[] byteArray = Encoding.UTF8.GetBytes("test");

            _mockBlobStorageConverter.Setup(c => c.GetBlobStreamAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(new MemoryStream(byteArray));
            _mockBlobStorageConverter.Setup(c => c.GetBlobBinaryDataAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(byteArray);
            _mockBlobStorageConverter.Setup(c => c.GetBlobStringAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync("test");
            _mockBlobStorageConverter.Setup(c => c.CreateBlobClient<BlobBaseClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<BlobBaseClient>().Object);
            _mockBlobStorageConverter.Setup(c => c.CreateBlobClient<BlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<BlobClient>().Object);
            _mockBlobStorageConverter.Setup(c => c.CreateBlobClient<BlockBlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<BlockBlobClient>().Object);
            _mockBlobStorageConverter.Setup(c => c.CreateBlobClient<PageBlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<PageBlobClient>().Object);
            _mockBlobStorageConverter.Setup(c => c.CreateBlobClient<AppendBlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<AppendBlobClient>().Object);
            _mockBlobStorageConverter.Setup(c => c.CreateBlobContainerClient(Constants.Connection, Constants.ContainerName)).Returns(new Mock<BlobContainerClient>().Object);

            var streamResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(Stream), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var byteArrayResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(Byte[]), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var stringResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blobClientResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(BlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blobBaseClientResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(BlobBaseClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blockBlobClientResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(BlockBlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var pageBlobClientResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(PageBlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var appendBlobClientResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(AppendBlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blobContainerClientResult = await _mockBlobStorageConverter.Object.ToTargetTypeAsync(typeof(BlobContainerClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);

            Assert.Equal(typeof(MemoryStream), streamResult.GetType());
            Assert.Equal(typeof(Byte[]), byteArrayResult.GetType());
            Assert.Equal(typeof(string), stringResult.GetType());
            Assert.Equal(typeof(BlobClient), blobClientResult.GetType().BaseType);
            Assert.Equal(typeof(BlobBaseClient), blobBaseClientResult.GetType().BaseType);
            Assert.Equal(typeof(BlockBlobClient), blockBlobClientResult.GetType().BaseType);
            Assert.Equal(typeof(PageBlobClient), pageBlobClientResult.GetType().BaseType);
            Assert.Equal(typeof(AppendBlobClient), appendBlobClientResult.GetType().BaseType);
            Assert.Equal(typeof(BlobContainerClient), blobContainerClientResult.GetType().BaseType);
        }

        [Fact]
        public void ToTargetTypeCollection_CloneToArray_Works()
        {
            IEnumerable<object> stringCollection = new List<object>() { "hello", "world"};
            string[] stringResult = (string[])_mockBlobStorageConverter.Object.ToTargetTypeCollection(stringCollection, nameof(BlobStorageConverter.CloneToArray), typeof(string));

            IEnumerable<object> pocoCollection = new List<object>() { new Book(), new Book() };
            Book[] pocoResult = (Book[])_mockBlobStorageConverter.Object.ToTargetTypeCollection(pocoCollection, nameof(BlobStorageConverter.CloneToArray), typeof(Book));

            IEnumerable<object> byteArraycollection = new List<object>() { Encoding.UTF8.GetBytes("hello"), Encoding.UTF8.GetBytes("world") };
            Byte[][] byteArrayResult = (Byte[][])_mockBlobStorageConverter.Object.ToTargetTypeCollection(byteArraycollection, nameof(BlobStorageConverter.CloneToArray), typeof(Byte[]));

            Assert.Equal(2, stringResult.Length);
            Assert.Equal(typeof(string), stringResult[0].GetType());

            Assert.Equal(2, pocoResult.Length);
            Assert.Equal(typeof(Book), pocoResult[0].GetType());

            Assert.Equal(2, byteArrayResult.Length);
            Assert.Equal(typeof(Byte[]), byteArrayResult[0].GetType());
        }

        [Fact]
        public void ToTargetTypeCollection_CloneToList_Works()
        {
            IEnumerable<object> stringCollection = new List<object>() { "hello", "world" };
            IEnumerable<string> stringResult = (IEnumerable<string>)_mockBlobStorageConverter.Object.ToTargetTypeCollection(stringCollection, nameof(BlobStorageConverter.CloneToArray), typeof(string));

            IEnumerable<object> pocoCollection = new List<object>() { new Book(), new Book() };
            IEnumerable<Book> pocoResult = (IEnumerable<Book>)_mockBlobStorageConverter.Object.ToTargetTypeCollection(pocoCollection, nameof(BlobStorageConverter.CloneToList), typeof(Book));

            IEnumerable<object> byteArraycollection = new List<object>() { Encoding.UTF8.GetBytes("hello"), Encoding.UTF8.GetBytes("world") };
            IEnumerable<Byte[]> byteArrayResult = (IEnumerable<Byte[]>)_mockBlobStorageConverter.Object.ToTargetTypeCollection(byteArraycollection, nameof(BlobStorageConverter.CloneToArray), typeof(Byte[]));

            Assert.Equal(2, stringResult.Count());
            Assert.Equal(typeof(string), stringResult.FirstOrDefault().GetType());

            Assert.Equal(2, pocoResult.Count());
            Assert.Equal(typeof(Book), pocoResult.FirstOrDefault().GetType());

            Assert.Equal(2, byteArrayResult.Count());
            Assert.Equal(typeof(Byte[]), byteArrayResult.FirstOrDefault().GetType());
        }


        [Fact]
        public async Task DeserializeToTargetObjectAsync_CorrectPoco_Works()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            string jsonstr = "{" + "\"Id\" : \"1\", \"Title\" : \"title\", \"Author\" : \"author\"}";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonstr);

            _mockBlobStorageConverter.Setup(c => c.GetBlobStreamAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(new MemoryStream(byteArray));

            var result = await _mockBlobStorageConverter.Object.DeserializeToTargetObjectAsync(typeof(Book), Constants.Connection, Constants.ContainerName, Constants.BlobName);

            Assert.Equal(typeof(Book), result.GetType());
        }

        [Fact]
        public async Task DeserializeToTargetObjectAsync_IncorrectPoco_Fails()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            string jsonstr = "{" + "\"Id\" : \"1\", \"Name\" : \"name\"}";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonstr);

            _mockBlobStorageConverter.Setup(c => c.GetBlobStreamAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(new MemoryStream(byteArray));

            try
            {
                var result = await _mockBlobStorageConverter.Object.DeserializeToTargetObjectAsync(typeof(Book), Constants.Connection, Constants.ContainerName, Constants.BlobName);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (Xunit.Sdk.FailException) { }
        }

        private BinaryData GetTestBinaryData()
        {
            return new BinaryData("{" + "\"BlobName\" : \"BlobName\"" + "}");
        }

        private BinaryData GetFullTestBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"ContainerName\" : \"ContainerName\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");
        }


        private GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData binaryData)
        {
            return new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });
        }

        private GrpcCollectionModelBindingData GetTestGrpcCollectionModelBindingData()
        {
            var modelBindingData = new ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "application/json"
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(modelBindingData);

            return new GrpcCollectionModelBindingData(array);
        }

        private Dictionary<string, string> GetTestContentDict()
        {
            return new Dictionary<string, string>
            {
                { Constants.Connection, Constants.Connection },
                { Constants.ContainerName, Constants.ContainerName },
                { Constants.BlobName, Constants.BlobName }
            };
        }
    }
}
