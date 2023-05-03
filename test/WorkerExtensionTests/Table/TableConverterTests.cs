// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;
using Azure.Data.Tables;
using System.Linq;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests.Table
{
    public class TableConverterTests
    {
        private Mock<TableConverter> _mockTableConverter;

        public TableConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var tableOptions = host.Services.GetService<IOptionsSnapshot<TablesBindingOptions>>();
            var logger = host.Services.GetService<ILogger<TableConverter>>();

            _mockTableConverter = new Mock<TableConverter>(tableOptions, logger);
            _mockTableConverter.CallBase = true;
        }

        [Fact]
        public async Task ConvertAsync_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(string), new object());

            var conversionResult = await _mockTableConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }


        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_ReturnsSuccess()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var result = new Mock<TableClient>();
            var context = new TestConverterContext(typeof(TableClient), source);
            _mockTableConverter
                .Setup(c => c.ToTargetTypeAsync(typeof(TableClient), Constants.Connection, Constants.TableName, Constants.PartitionKey, Constants.RowKey))
                .ReturnsAsync(result.Object);

            var conversionResult = await _mockTableConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsCollectionModelBindingData_ReturnsSuccess()
        {
            object source = GetTestGrpcCollectionModelBindingData();
            var context = new TestConverterContext(typeof(TableEntity), source);
            var result = new Mock<IEnumerable<TableEntity>>();
            _mockTableConverter
                .Setup(c => c.ConvertFromCollectionBindingDataAsync(context, (Worker.Core.CollectionModelBindingData)source))
                .Returns(new ValueTask<ConversionResult>(ConversionResult.Success(result.Object)));

            var conversionResult = await _mockTableConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }


        [Fact]
        public async Task ConvertAsync_SourceAsModelBindingData_ReturnsUnhandled()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var context = new TestConverterContext(typeof(TableClient), source);
            _mockTableConverter
                .Setup(c => c.ToTargetTypeAsync(typeof(TableClient), Constants.Connection, Constants.TableName, Constants.PartitionKey, Constants.RowKey))
                .ThrowsAsync(new Exception());

            var conversionResult = await _mockTableConverter.Object.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Theory]
        [InlineData(Constants.TablesExtensionName, true)]
        [InlineData(" ", false)]
        [InlineData("incorrect-value", false)]
        public void IsTableExtension_MatchesExpectedOutput(string sourceVal, bool expectedResult)
        {
            var grpcModelBindingData = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = sourceVal,
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "application/json"
            });

            var result = _mockTableConverter.Object.IsTableExtension(grpcModelBindingData);

            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void GetBindingDataContent_IncompleteGrpcModelBindingData_ReturnsNull()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());

            var result = _mockTableConverter.Object.GetBindingDataContent(grpcModelBindingData);

            result.TryGetValue(Constants.TableName, out var tableName);

            Assert.Single(result);
            Assert.True(tableName is null);
        }

        [Fact]
        public void GetBindingDataContent_UnSupportedContentType_Throws()
        {
            var grpcModelBindingData = new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = Constants.TablesExtensionName,
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "NotSupported"
            });

            try
            {
                var dict = _mockTableConverter.Object.GetBindingDataContent(grpcModelBindingData);
                Assert.Fail("Test fails as the expected exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
            }
        }

        [Fact]
        public async Task ToTargetTypeAsync_Works()
        {
            object source = GetTestGrpcModelBindingData(GetFullTestBinaryData());
            var byteArray = Encoding.UTF8.GetBytes("test");

            _mockTableConverter.Setup(c => c.GetTableClient(Constants.Connection, Constants.TableName)).Returns(new Mock<TableClient>().Object);
            _mockTableConverter.Setup(c => c.GetTableEntity(Constants.Connection, Constants.TableName, Constants.PartitionKey, Constants.RowKey)).Returns(new TableEntity());

            var tableClientResult = await _mockTableConverter.Object.ToTargetTypeAsync(typeof(TableClient), Constants.Connection, Constants.TableName, Constants.PartitionKey, Constants.RowKey);
            var tableEntityResult = await _mockTableConverter.Object.ToTargetTypeAsync(typeof(TableEntity), Constants.Connection, Constants.TableName, Constants.PartitionKey, Constants.RowKey);

            Assert.Equal(typeof(TableClient), tableClientResult.GetType().BaseType);
            Assert.Equal(typeof(TableEntity), tableEntityResult.GetType());
        }

        [Fact]
        public void ToTargetTypeCollection_CloneToEnumerable_Works()
        {
            IEnumerable<object> tableCollection = new List<object>() { new TableEntity(), new TableEntity() };
            var tableCollectionResult = (IEnumerable<TableEntity>)_mockTableConverter.Object.ToTargetTypeCollection(tableCollection, nameof(TableConverter.CloneToEnumerable), typeof(TableEntity));
            var result = tableCollectionResult.ToList();
            Assert.Equal(2, tableCollectionResult.Count());
            Assert.Equal(typeof(List<TableEntity>), result.GetType());
        }

        private BinaryData GetTestBinaryData()
        {
            return new BinaryData("{" + "\"Connection\" : \"Connection\"" + "}");
        }

        private BinaryData GetFullTestBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"," +
                 "\"RowKey\" : \"RowKey\"," +
                 "\"Filter\" : \"Filter\"," +
                 "\"Take\" : 0" +
                "}");
        }


        private GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData binaryData)
        {
            return new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageTables",
                Content = ByteString.CopyFrom(binaryData),
                ContentType = "application/json"
            });
        }

        private GrpcCollectionModelBindingData GetTestGrpcCollectionModelBindingData()
        {
            var modelBindingData = new ModelBindingData()
            {
                Version = "1.0",
                Source = "AzureStorageTables",
                Content = ByteString.CopyFrom(GetFullTestBinaryData()),
                ContentType = "application/json"
            };

            var array = new CollectionModelBindingData();
            array.ModelBindingData.Add(modelBindingData);

            return new GrpcCollectionModelBindingData(array);
        }
    }
}
