// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Table
{
    public class TableObjectConverterTests
    {
        private TableObjectConverter _tableConverter;
        private Mock<TableServiceClient> _mockTableServiceClient;

        public TableObjectConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<TableObjectConverter>>();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();

            _mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(_mockTableServiceClient.Object);

            var mockTablesOptionsMonitor = new Mock<IOptionsMonitor<TablesBindingOptions>>();
            mockTablesOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            _tableConverter = new TableObjectConverter(workerOptions, mockTablesOptionsMonitor.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_SinglePocoEntity_ReturnsSuccess()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(typeof(MyEntity), source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(typeof(MyEntity), conversionResult.Value.GetType());
        }

        [Fact]
        public async Task ConvertAsync_CollectionPocoEntity_ReturnsSuccess()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetEntityWithoutRowKeyBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(typeof(IEnumerable<MyEntity>), source);
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
        public async Task ConvertAsync_CollectionPocoEntity_WithRowKey_ReturnsSuccess()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(typeof(IEnumerable<MyEntity>), source);
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
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(IEnumerable<MyEntity>), null);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SingleTableEntity_NullTargetType_ReturnsFailed()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(null, source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }
    }

    class MyEntity
    {
        public MyEntity() { }

        public MyEntity(string pk, string rk)
        {
            PartitionKey = pk;
            RowKey = rk;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
}
