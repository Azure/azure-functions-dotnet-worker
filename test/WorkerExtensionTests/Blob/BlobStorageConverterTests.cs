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
        private Mock<BlobStorageConverter> mock = new(MockBehavior.Strict);

        public BlobStorageConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var _workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            var _blobOptions = host.Services.GetService<IOptionsSnapshot<BlobStorageBindingOptions>>();
            var logger = host.Services.GetService<ILogger<BlobStorageConverter>>();
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
        public async Task ConvertAsync_SourceAsModelBindingData_Success()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            mock.Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync("abc");

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsCollectionModelBindingData_Success()
        {
            object source = GetTestGrpcCollectionModelBindingData();
            var context = new TestConverterContext(typeof(string), source);
            Worker.Core.CollectionModelBindingData collection = (Worker.Core.CollectionModelBindingData)context.Source;
            var res = new ValueTask<ConversionResult>(ConversionResult.Success("abc"));
            mock.Setup(c => c.ConvertFromCollectionBindingDataAsync(context, collection)).Returns(res);

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_Fails()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            mock.Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName)).ThrowsAsync(new Exception());

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertFromBindingDataAsync_Success()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            var dict = mock.Object.GetBindingDataContent((Worker.Core.ModelBindingData)source);

            mock.Setup(c => c.ConvertModelBindingDataAsync(dict, typeof(string), (Worker.Core.ModelBindingData)source)).ReturnsAsync(new ValueTask<ConversionResult>(ConversionResult.Success("abc")));

            var conversionResult = await mock.Object.ConvertFromBindingDataAsync(context, (Worker.Core.ModelBindingData)source);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertFromBindingDataAsync_Fails()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            var dict = mock.Object.GetBindingDataContent((Worker.Core.ModelBindingData)source);

            mock.Setup(c => c.ConvertModelBindingDataAsync(dict, typeof(string), (Worker.Core.ModelBindingData)source)).ThrowsAsync(new Exception());

            var conversionResult = await mock.Object.ConvertFromBindingDataAsync(context, (Worker.Core.ModelBindingData)source);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertFromBindingDataAsync_Unhandled()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(string), source);
            var dict = mock.Object.GetBindingDataContent((Worker.Core.ModelBindingData)source);

            mock.Setup(c => c.ConvertModelBindingDataAsync(dict, typeof(string), (Worker.Core.ModelBindingData)source)).ReturnsAsync(null);

            var conversionResult = await mock.Object.ConvertFromBindingDataAsync(context, (Worker.Core.ModelBindingData)source);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Theory]
        [InlineData(Constants.BlobExtensionName, true)]
        [InlineData(" ", false)]
        [InlineData("incorrect.value", false)]
        public void IsBlobExtension_GrpcModelBindingData_Works(string sourceVal, bool expectedResult)
        {
            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = sourceVal,
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "application/json"
            });

            var result = mock.Object.IsBlobExtension(grpcModelBindingData);

            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void GetBindingDataContent_GrpcModelBindingData_Works()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetFullTestBinaryData());

            var result = mock.Object.GetBindingDataContent(grpcModelBindingData);

            Assert.Equal(3, result.Count);

            result.TryGetValue(Constants.Connection, out var connectionName);
            result.TryGetValue(Constants.ContainerName, out var containerName);
            result.TryGetValue(Constants.BlobName, out var blobName);

            Assert.Equal(Constants.Connection, connectionName);
            Assert.Equal(Constants.ContainerName, containerName);
            Assert.Equal(Constants.BlobName, blobName);
        }

        [Fact]
        public void GetBindingDataContent_UnSupportedContentType_Throws()
        {
            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = Constants.BlobExtensionName,
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "NotSupported"
            });

            try
            {
                var dict = mock.Object.GetBindingDataContent(grpcModelBindingData);

               // var result = await mock.Object.ConvertModelBindingDataAsync(dict, typeof(string), grpcModelBindingData);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
            }
        }

        [Fact]
        public async Task ConvertModelBindingDataAsync_GrpcModelBindingData_Throws()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());

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
        }

        [Fact]
        public async Task ConvertModelBindingDataAsync_GrpcModelBindingData_Works()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetFullTestBinaryData());

            var dict = mock.Object.GetBindingDataContent(grpcModelBindingData);

            mock.Setup(c => c.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync("abc");

            var result = await mock.Object.ConvertModelBindingDataAsync(dict, typeof(string), grpcModelBindingData);

            Assert.Equal(typeof(string), result.GetType());

        }

        [Fact]
        public async Task ToTargetTypeAsync_Works()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());

            byte[] byteArray = Encoding.UTF8.GetBytes("test");
            mock.Setup(c => c.GetBlobStreamAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(new MemoryStream(byteArray));
            mock.Setup(c => c.GetBlobBinaryDataAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(byteArray);
            mock.Setup(c => c.GetBlobStringAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync("test");
            mock.Setup(c => c.CreateBlobClient<BlobBaseClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<BlobBaseClient>().Object);
            mock.Setup(c => c.CreateBlobClient<BlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<BlobClient>().Object);
            mock.Setup(c => c.CreateBlobClient<BlockBlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<BlockBlobClient>().Object);
            mock.Setup(c => c.CreateBlobClient<PageBlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<PageBlobClient>().Object);
            mock.Setup(c => c.CreateBlobClient<AppendBlobClient>(Constants.Connection, Constants.ContainerName, Constants.BlobName)).Returns(new Mock<AppendBlobClient>().Object);
            mock.Setup(c => c.CreateBlobContainerClient(Constants.Connection, Constants.ContainerName)).Returns(new Mock<BlobContainerClient>().Object);

            var streamResult = await mock.Object.ToTargetTypeAsync(typeof(Stream), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var byteArrayResult = await mock.Object.ToTargetTypeAsync(typeof(Byte[]), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var stringResult = await mock.Object.ToTargetTypeAsync(typeof(string), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blobClientResult = await mock.Object.ToTargetTypeAsync(typeof(BlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blobBaseClientResult = await mock.Object.ToTargetTypeAsync(typeof(BlobBaseClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blockBlobClientResult = await mock.Object.ToTargetTypeAsync(typeof(BlockBlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var pageBlobClientResult = await mock.Object.ToTargetTypeAsync(typeof(PageBlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var appendBlobClientResult = await mock.Object.ToTargetTypeAsync(typeof(AppendBlobClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);
            var blobContainerClientResult = await mock.Object.ToTargetTypeAsync(typeof(BlobContainerClient), Constants.Connection, Constants.ContainerName, Constants.BlobName);

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
            string[] stringResult = (string[])mock.Object.ToTargetTypeCollection(stringCollection, nameof(BlobStorageConverter.CloneToArray), typeof(string));

            IEnumerable<object> pocoCollection = new List<object>() { new Book(), new Book() };
            Book[] pocoResult = (Book[])mock.Object.ToTargetTypeCollection(pocoCollection, nameof(BlobStorageConverter.CloneToArray), typeof(Book));

            IEnumerable<object> byteArraycollection = new List<object>() { Encoding.UTF8.GetBytes("hello"), Encoding.UTF8.GetBytes("world") };
            Byte[][] byteArrayResult = (Byte[][])mock.Object.ToTargetTypeCollection(byteArraycollection, nameof(BlobStorageConverter.CloneToArray), typeof(Byte[]));

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
            IEnumerable<string> stringResult = (IEnumerable<string>)mock.Object.ToTargetTypeCollection(stringCollection, nameof(BlobStorageConverter.CloneToArray), typeof(string));

            IEnumerable<object> pocoCollection = new List<object>() { new Book(), new Book() };
            IEnumerable<Book> pocoResult = (IEnumerable<Book>)mock.Object.ToTargetTypeCollection(pocoCollection, nameof(BlobStorageConverter.CloneToList), typeof(Book));

            IEnumerable<object> byteArraycollection = new List<object>() { Encoding.UTF8.GetBytes("hello"), Encoding.UTF8.GetBytes("world") };
            IEnumerable<Byte[]> byteArrayResult = (IEnumerable<Byte[]>)mock.Object.ToTargetTypeCollection(byteArraycollection, nameof(BlobStorageConverter.CloneToArray), typeof(Byte[]));

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

            mock.Setup(c => c.GetBlobStreamAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(new MemoryStream(byteArray));

            var result = await mock.Object.DeserializeToTargetObjectAsync(typeof(Book), Constants.Connection, Constants.ContainerName, Constants.BlobName);

            Assert.Equal(typeof(Book), result.GetType());
        }

        [Fact]
        public async Task DeserializeToTargetObjectAsync_IncorrectPoco_Fails()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            string jsonstr = "{" + "\"Id\" : \"1\", \"Name\" : \"name\"}";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonstr);

            mock.Setup(c => c.GetBlobStreamAsync(Constants.Connection, Constants.ContainerName, Constants.BlobName)).ReturnsAsync(new MemoryStream(byteArray));

            try
            {
                var result = await mock.Object.DeserializeToTargetObjectAsync(typeof(Book), Constants.Connection, Constants.ContainerName, Constants.BlobName);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(Xunit.Sdk.FailException), ex.GetType());
            }
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
            return new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });
        }

        private GrpcCollectionModelBindingData GetTestGrpcCollectionModelBindingData()
        {
            var modelBindingData = new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "application/json"
            };

            var array = new Worker.Grpc.Messages.CollectionModelBindingData();
            array.ModelBindingData.Add(modelBindingData);
            var collection = new GrpcCollectionModelBindingData(array);

            return collection;
        }
    }
}
