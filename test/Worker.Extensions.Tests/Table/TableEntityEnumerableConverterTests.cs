// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Table
{
    public class TableEntityEnumerableConverterTests
    {
        private TableEntityEnumerableConverter _tableConverter;
        private Mock<TableServiceClient> _mockTableServiceClient;

        public TableEntityEnumerableConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<TableEntityEnumerableConverter>>();

            _mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(_mockTableServiceClient.Object);

            var mockTablesOptionsSnapshot = new Mock<IOptionsSnapshot<TablesBindingOptions>>();
            mockTablesOptionsSnapshot
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            _tableConverter = new TableEntityEnumerableConverter(mockTablesOptionsSnapshot.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(string), new object());

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }


        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_ReturnsUnhandled()
        {
            object source = GetTestGrpcModelBindingData(GetTableClientBinaryData());
            var result = new Mock<TableClient>();
            var context = new TestConverterContext(typeof(TableClient), source);

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns((TableClient)result.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsCollectionModelBindingData_ReturnsSuccess()
        {
            object source = GetTestGrpcModelBindingData(GetTableEntityBinaryData());
            var context = new TestConverterContext(typeof(IEnumerable<TableEntity>), source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var expectedOutput = Page<TableEntity>.FromValues(new List<TableEntity> { new TableEntity("partitionKey", "rowKey") }, continuationToken: null, mockResponse.Object);

            tableClient
                .Setup(c => c.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, default))
                .Returns(AsyncPageable<TableEntity>.FromPages(new List<Page<TableEntity>> { expectedOutput }));

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsCollectionModelBindingData_TableEntity_ReturnsSuccess()
        {
            object source = GetTestGrpcModelBindingData(GetTableEntityBinaryData());
            var context = new TestConverterContext(typeof(TableEntity), source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsCollectionModelBindingData_BadTableEntity_ReturnsSuccess()
        {
            object source = GetTestGrpcModelBindingData(GetBadEntityBinaryData());
            var context = new TestConverterContext(typeof(TableEntity), source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ContentSource_AsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(IEnumerable<TableEntity>), new Object());

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(IEnumerable<TableEntity>), null);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotCosmosExtension_ReturnsUnhandled()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTableEntityBinaryData(), source: "anotherExtensions");
            var context = new TestConverterContext(typeof(IEnumerable<TableEntity>), grpcModelBindingData);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTableEntityBinaryData(), contentType: "binary");
            var context = new TestConverterContext(typeof(IEnumerable<TableEntity>), grpcModelBindingData);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type. Currently only 'application/json' is supported.", conversionResult.Error.Message);
        }

        private BinaryData GetTableClientBinaryData()
        {
            return new BinaryData("{" +
                "\"TableName\" : \"TableName\"" +
                "}");
        }

        private BinaryData GetTableEntityBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"," +
                "\"RowKey\" : \"RowKey\"" +
                "}");
        }

        private BinaryData GetBadEntityBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"" +
                "}");
        }

        private GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData binaryData, string source = "AzureStorageTables", string contentType = "application/json")
        {
            return new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = source,
                Content = ByteString.CopyFrom(binaryData),
                ContentType = contentType
            });
        }
    }
}
