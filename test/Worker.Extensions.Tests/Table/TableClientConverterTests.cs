// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class TableClientConverterTests
    {
        private TableClientConverter _tableConverter;
        private Mock<TableServiceClient> _mockTableServiceClient;

        public TableClientConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<TableClientConverter>>();

            _mockTableServiceClient = new Mock<TableServiceClient>();

            var mockTableOptions = new Mock<TablesBindingOptions>();
            mockTableOptions
                .Setup(m => m.CreateClient())
                .Returns(_mockTableServiceClient.Object);

            var mockTablesOptionsMonitor = new Mock<IOptionsMonitor<TablesBindingOptions>>();
            mockTablesOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockTableOptions.Object);

            _tableConverter = new TableClientConverter(mockTablesOptionsMonitor.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_SingleTableClient_ReturnsSuccess()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableClientBinaryData(), "AzureStorageTables");
            var result = new Mock<TableClient>();
            var context = new TestConverterContext(typeof(TableClient), source);

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns((TableClient)result.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_CollectionTableClient_ReturnsUnhandled()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableEntityBinaryData(), "AzureStorageTables");
            var context = new TestConverterContext(typeof(IEnumerable<TableClient>), source);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_GetTableClient_Throws_ReturnsFailed()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableClientBinaryData(), "AzureStorageTables");
            var result = new Mock<TableClient>();
            var context = new TestConverterContext(typeof(TableClient), source);

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Throws(new Exception());

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_InvalidModelBindingData_ReturnsFailed()
        {
            object source = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetWrongBinaryData(), "AzureStorageTables");
            var result = new Mock<TableClient>();
            var context = new TestConverterContext(typeof(TableClient), source);

            _mockTableServiceClient
                .Setup(c => c.GetTableClient(Constants.TableName))
                .Returns(result.Object);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ContentSource_AsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(TableClient), new Object());

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(TableClient), null);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotTableExtension_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableClientBinaryData(), source: "anotherExtensions");
            var context = new TestConverterContext(typeof(TableClient), grpcModelBindingData);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected binding source 'anotherExtensions'. Only 'AzureStorageTables' is supported.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(TableTestHelper.GetTableClientBinaryData(), "AzureStorageTables", contentType: "binary");
            var context = new TestConverterContext(typeof(TableClient), grpcModelBindingData);

            var conversionResult = await _tableConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type 'binary'. Only 'application/json' is supported.", conversionResult.Error.Message);
        }
    }
}
