// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.Azure;
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
        private const string _sourceString = "hello";
        private static readonly byte[] _sourceBytes = Encoding.UTF8.GetBytes(_sourceString);
        private static readonly ReadOnlyMemory<byte> _sourceMemory = new ReadOnlyMemory<byte>(_sourceBytes);
        private BlobStorageConverter _converter;

        private static readonly IEnumerable<ReadOnlyMemory<byte>> _sourceMemoryEnumerable = new RepeatedField<ReadOnlyMemory<byte>>() { _sourceMemory };
        //private static readonly RepeatedField<string> _sourceStringEnumerable = new RepeatedField<string>() { _sourceString };
        //private static readonly RepeatedField<double> _sourceDoubleEnumerable = new RepeatedField<double>() { 1.0 };
        //private static readonly RepeatedField<long> _sourceLongEnumerable = new RepeatedField<long>() { 2000 };
        //private Mock<BlobStorageConverter> mock;
        private Mock<BlobStorageConverter> mock = new(MockBehavior.Strict);

        public BlobStorageConverterTests()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults((WorkerOptions options) =>
                {
                    options.Capabilities.Remove("HandlesWorkerTerminateMessage");
                    options.Capabilities.Add("SomeNewCapability", bool.TrueString);
                }).Build();

            var _workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();
            var _blobOptions = host.Services.GetService<IOptionsSnapshot<BlobStorageBindingOptions>>();
            var logger = host.Services.GetService<ILogger<BlobStorageConverter>>();

            _converter = new BlobStorageConverter(_workerOptions, _blobOptions, logger);
            mock = new Mock<BlobStorageConverter>(_workerOptions, _blobOptions, logger);
            mock.CallBase = true;
        }

        [Fact]
        public async Task StorageConverter()
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            object source = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData() 
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var _sourceBytes1 = Encoding.UTF8.GetBytes(new Object().ToString()); //source.ToString());
            var _sourceMemory1 = new ReadOnlyMemory<byte>(_sourceBytes1);

            var context = new TestConverterContext(typeof(string), new Object()); // source);//_sourceMemory1);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            //TestUtility.AssertIsTypeAndConvert<string>(conversionResult.Value);
        }

        [Fact]
        public async Task StorageConverterWithData()
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            object source = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            var context = new TestConverterContext(typeof(string), source);

            mock.Setup(c => c.ToTargetTypeAsync(typeof(string),"connection", "container", "BlobName")).ReturnsAsync("abc");

            mock.Setup(c => c.GetBlobStringAsync("connection", "container", "BlobName")).ReturnsAsync("abc");

            var conversionResult = await mock.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            //TestUtility.AssertIsTypeAndConvert<string>(conversionResult.Value);
        }


        [Fact]
        public void GetBindingDataContentTest()
        {
            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

           // var context = new TestConverterContext(typeof(string), new Object()); // source);//_sourceMemory1);

            var conversionResult = _converter.GetBindingDataContent(grpcModelBindingData);

            Assert.Equal(3, conversionResult.Count);
            conversionResult.TryGetValue(Constants.Connection, out var connectionName);
            Assert.Equal("connection", connectionName);

            //Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            //TestUtility.AssertIsTypeAndConvert<string>(conversionResult.Value);
        }

        [Fact]
        public async Task ToTargetTypeTest()
        {
            var res = await _converter.ToTargetTypeAsync(typeof(Book), "connectionName", "containerName", "blobName");

            var binaryData = new BinaryData("{" +
                "\"Connection\" : \"connection\"," +
                "\"ContainerName\" : \"container\"," +
                "\"BlobName\" : \"BlobName\"" +
                "}");

            var grpcModelBindingData = new GrpcModelBindingData(new Worker.Grpc.Messages.ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageBlobs",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });

            // var context = new TestConverterContext(typeof(string), new Object()); // source);//_sourceMemory1);

            var conversionResult = _converter.GetBindingDataContent(grpcModelBindingData);

            Assert.Equal(3, conversionResult.Count);
            conversionResult.TryGetValue(Constants.Connection, out var connectionName);
            Assert.Equal("connection", connectionName);

            //Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            //TestUtility.AssertIsTypeAndConvert<string>(conversionResult.Value);
        }

    }
}
