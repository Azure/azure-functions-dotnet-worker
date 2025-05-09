﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Serialization;
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
    public class TablePocoConverterTests
    {
        private TablePocoConverter _tableConverter;
        private Mock<TableServiceClient> _mockTableServiceClient;

        public TablePocoConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<TablePocoConverter>>();

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

            _tableConverter = new TablePocoConverter(workerOptions, mockTablesOptionsMonitor.Object, logger);
        }

        [Theory]
        [InlineData(typeof(MyEntity))]
        [InlineData(typeof(MyTableEntity))]
        public async Task ConvertAsync_SinglePocoEntity_ReturnsSuccess(Type targetType)
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(targetType, source);
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
            Assert.Equal(targetType, conversionResult.Value.GetType());
        }

        [Theory]
        [InlineData(typeof(IEnumerable<MyEntity>))]
        [InlineData(typeof(IEnumerable<MyTableEntity>))]
        public async Task ConvertAsync_CollectionPocoEntity_ReturnsSuccess(Type targetType)
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetEntityWithoutRowKeyBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(targetType, source);
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

        [Theory]
        [InlineData(typeof(IEnumerable<MyEntity>))]
        [InlineData(typeof(IEnumerable<MyTableEntity>))]
        public async Task ConvertAsync_CollectionPocoEntity_WithRowKey_ReturnsSuccess(Type targetType)
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(targetType, source);
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

        [Fact]
        public async Task ConvertAsync_MyEntityNewField_NewtonSoftJsonSerializer_ReturnsSuccess()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { options.Serializer = new NewtonsoftJsonObjectSerializer(); }).Build();
            var logger = host.Services.GetService<ILogger<TablePocoConverter>>();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();

            var mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(mockTableServiceClient.Object);

            var mockTablesOptionsMonitor = new Mock<IOptionsMonitor<TablesBindingOptions>>();
            mockTablesOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            var tableConverter = new TablePocoConverter(workerOptions, mockTablesOptionsMonitor.Object, logger);

            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityWithNewFieldBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(typeof(MyTableEntityWithField), source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(typeof(MyTableEntityWithField), conversionResult.Value.GetType());
        }

        [Theory]
        [InlineData(typeof(MyEntity))]
        [InlineData(typeof(MyTableEntity))]
        public async Task ConvertAsync_NewtonSoftJsonSerializer_ReturnsSuccess(Type targetType)
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { options.Serializer = new NewtonsoftJsonObjectSerializer(); }).Build();
            var logger = host.Services.GetService<ILogger<TablePocoConverter>>();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();

            var mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(mockTableServiceClient.Object);

            var mockTablesOptionsMonitor = new Mock<IOptionsMonitor<TablesBindingOptions>>();
            mockTablesOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            var tableConverter = new TablePocoConverter(workerOptions, mockTablesOptionsMonitor.Object, logger);

            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(targetType, source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(targetType, conversionResult.Value.GetType());
        }

        [Theory]
        [InlineData(typeof(MyEntity))]
        [InlineData(typeof(MyTableEntity))]
        public async Task ConvertAsync_JsonSerializer_ReturnsSuccess(Type t)
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { options.Serializer = new JsonObjectSerializer (); }).Build();
            var logger = host.Services.GetService<ILogger<TablePocoConverter>>();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();

            var mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(mockTableServiceClient.Object);

            var mockTablesOptionsMonitor = new Mock<IOptionsMonitor<TablesBindingOptions>>();
            mockTablesOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            var tableConverter = new TablePocoConverter(workerOptions, mockTablesOptionsMonitor.Object, logger);

            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(t, source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var conversionResult = await tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(t, conversionResult.Value.GetType());
        }

        [Theory]
        [InlineData(typeof(IEnumerable<MyEntity>))]
        [InlineData(typeof(IEnumerable<MyTableEntity>))]
        public async Task ConvertAsync_CollectionPocoEntity_NewtonSoftSerializer_ReturnsSuccess(Type t)
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { options.Serializer = new NewtonsoftJsonObjectSerializer(); }).Build();
            var logger = host.Services.GetService<ILogger<TablePocoConverter>>();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>();

            var mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(mockTableServiceClient.Object);

            var mockTablesOptionsMonitor = new Mock<IOptionsMonitor<TablesBindingOptions>>();
            mockTablesOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            var tableConverter = new TablePocoConverter(workerOptions, mockTablesOptionsMonitor.Object, logger);

            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(t, source);
            var mockResponse = new Mock<Response>();
            var tableClient = new Mock<TableClient>();

            tableClient
                .Setup(c => c.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(Response.FromValue(new TableEntity(It.IsAny<string>(), It.IsAny<string>()), mockResponse.Object));

            mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(tableClient.Object);

            var expectedOutput = Page<TableEntity>.FromValues(new List<TableEntity> { new TableEntity("partitionKey", "rowKey") }, continuationToken: null, mockResponse.Object);

            tableClient
                .Setup(c => c.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, default))
                .Returns(AsyncPageable<TableEntity>.FromPages(new List<Page<TableEntity>> { expectedOutput }));

            var conversionResult = await tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
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

        class MyTableEntity : ITableEntity
        {
            public MyTableEntity() { }

            public MyTableEntity(string pk, string rk)
            {
                PartitionKey = pk;
                RowKey = rk;
            }

            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }

        class MyTableEntityWithField : ITableEntity
        {
            public MyTableEntityWithField() { }

            public MyTableEntityWithField(string pk, string rk)
            {
                PartitionKey = pk;
                RowKey = rk;
                NewField = "test";
            }

            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
            public string NewField { get; set; }
        }
    }
}
