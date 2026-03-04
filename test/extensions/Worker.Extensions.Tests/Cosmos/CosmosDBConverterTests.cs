// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Cosmos
{
    public class CosmosDBConverterTests
    {
        private CosmosDBConverter _cosmosDBConverter;
        private Mock<CosmosClient> _mockCosmosClient;

        public CosmosDBConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<CosmosDBConverter>>();

            _mockCosmosClient = new Mock<CosmosClient>();

            var mockCosmosOptions = new Mock<CosmosDBBindingOptions>();
            mockCosmosOptions
                .Setup(m => m.GetClient(It.IsAny<string>()))
                .Returns(_mockCosmosClient.Object);

            var mockCosmosOptionsMonitor = new Mock<IOptionsMonitor<CosmosDBBindingOptions>>();
            mockCosmosOptionsMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockCosmosOptions.Object);

            _cosmosDBConverter = new CosmosDBConverter(mockCosmosOptionsMonitor.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_CosmosClient_ReturnsSuccess()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "CosmosDB");
            var context = new TestConverterContext(typeof(CosmosClient), grpcModelBindingData);

            _mockCosmosClient.Setup(m => m.Endpoint).Returns(new Uri("https://www.example.com"));

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            var expectedClient = (CosmosClient)conversionResult.Value;
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(new Uri("https://www.example.com"), expectedClient.Endpoint);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_DatabaseClient_ReturnsSuccess()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "CosmosDB");
            var context = new TestConverterContext(typeof(Database), grpcModelBindingData);

            var _mockDatabase = new Mock<Database>();
            _mockDatabase.Setup(m => m.Id).Returns("testId");

            _mockCosmosClient
                .Setup(m => m.GetDatabase(It.IsAny<string>()))
                .Returns(_mockDatabase.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            var expectedDatabase = (Database)conversionResult.Value;
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("testId", expectedDatabase.Id);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_ContainerClient_ReturnsSuccess()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "CosmosDB");
            var context = new TestConverterContext(typeof(Container), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer.Setup(m => m.Id).Returns("testId");

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            var expectedContainer = (Container)conversionResult.Value;
            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("testId", expectedContainer.Id);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_SinglePOCO_ReturnsSuccess()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var json = @"{""id"":""1"",""description"":""Take out the rubbish""}";
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var mockResponse = new Mock<ResponseMessage>();
            mockResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            mockResponse.Setup(x => x.Content).Returns(expectedStream);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemStreamAsync(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, CancellationToken.None))
                .ReturnsAsync(mockResponse.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);
            var result = conversionResult.Value as ToDoItem;

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal("Take out the rubbish", result.Description);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_SinglePOCO_WithoutId_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(partitionKey: "1"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockContainer = new Mock<Container>();

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("The 'Id' and 'PartitionKey' properties of a CosmosDB single-item input binding cannot be null or empty.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_SinglePOCO_WithoutPK_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(id: "1"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockContainer = new Mock<Container>();

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("The 'Id' and 'PartitionKey' properties of a CosmosDB single-item input binding cannot be null or empty.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_IEnumerablePOCO_ReturnsSuccess()
        {
            var query = "SELECT * FROM TodoItems t WHERE t.id = @id";
            var queryParams = @"{""@id"":""1""}";
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(query: query, queryParams: queryParams), "CosmosDB");
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            var json = @"{""Documents"":[{""id"":""1"",""description"":""Take out the rubbish""},{""id"":""2"",""description"":""Write unit tests for cosmos converter""}],""_rid"":""ltwyAJGNSgs="",""_count"":2}";
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var mockFeedResponse = new Mock<ResponseMessage>();
            mockFeedResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            mockFeedResponse.Setup(x => x.Content).Returns(expectedStream);

            var mockFeedIterator = new Mock<FeedIterator>();
            mockFeedIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            mockFeedIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(mockFeedResponse.Object);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(), default, It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);
            var result = conversionResult.Value as IList<ToDoItem>;

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(2, result.Count);
            Assert.Equal("Take out the rubbish", result[0].Description);
            Assert.Equal("Write unit tests for cosmos converter", result[1].Description);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_ArrayPOCO_ReturnsSuccess()
        {
            var query = "SELECT * FROM TodoItems t WHERE t.id = @id";
            var queryParams = @"{""@id"":""1""}";
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(query: query, queryParams: queryParams), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem[]), grpcModelBindingData);

            var json = @"{""Documents"":[{""id"":""1"",""description"":""Take out the rubbish""},{""id"":""2"",""description"":""Write unit tests for cosmos converter""}],""_rid"":""ltwyAJGNSgs="",""_count"":2}";
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var mockFeedResponse = new Mock<ResponseMessage>();
            mockFeedResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            mockFeedResponse.Setup(x => x.Content).Returns(expectedStream);

            var mockFeedIterator = new Mock<FeedIterator>();
            mockFeedIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            mockFeedIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(mockFeedResponse.Object);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(), default, It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);
            var result = conversionResult.Value as ToDoItem[];

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(2, result.Length);
            Assert.Equal("Take out the rubbish", result[0].Description);
            Assert.Equal("Write unit tests for cosmos converter", result[1].Description);
        }

        [Fact]
        public async Task ConvertAsync_Container_NullFeedIterator_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "CosmosDB");
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer.Setup(m => m.Id).Returns("testId");
            mockContainer
                .Setup(m => m.GetItemQueryIterator<ToDoItem>(It.IsAny<QueryDefinition>(), null, null))
                .Returns<FeedIterator<ToDoItem>>(null);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Contains("Unable to retrieve documents for container 'testId'.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ContainerWithSqlQuery_NullFeedIterator_ReturnsFailed()
        {
            var query = "SELECT * FROM TodoItems t WHERE t.id = @id";
            var queryParams = @"{""@id"":""1""}";
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(query: query, queryParams: queryParams), "CosmosDB");
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer.Setup(m => m.Id).Returns("testId");
            mockContainer
                .Setup(m => m.GetItemQueryIterator<ToDoItem>(It.IsAny<QueryDefinition>(), null, null))
                .Returns((FeedIterator<ToDoItem>)null);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Contains($"Unable to retrieve documents for container 'testId'.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ContentSource_AsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(CosmosClient), new Object());

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingData_Null_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(CosmosClient), null);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ThrowsException_ReturnsFailure()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "CosmosDB");
            var context = new TestConverterContext(typeof(Database), grpcModelBindingData);

            _mockCosmosClient
                .Setup(m => m.GetDatabase(It.IsAny<string>()))
                .Throws(new Exception());

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotCosmosExtension_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "anotherExtensions");
            var context = new TestConverterContext(typeof(CosmosClient), grpcModelBindingData);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected binding source 'anotherExtensions'. Only 'CosmosDB' is supported.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(), "CosmosDB", contentType: "binary");
            var context = new TestConverterContext(typeof(CosmosClient), grpcModelBindingData);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type 'binary'. Only 'application/json' is supported.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_CosmosContainerIsNull_ThrowsException_ReturnsFailure()
        {
            object grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(container: "myContainer"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<Container>(null);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal($"Unable to create Cosmos container client for 'myContainer'.", conversionResult.Error.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadRequest)]
        public async Task ConvertAsync_POCO_IdProvided_NotSuccessStatus_ThrowsException_ReturnsFailure(HttpStatusCode httpStatusCode)
        {
            object grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockResponse = new Mock<ResponseMessage>();
            var cosmosException = new CosmosException("test failure", httpStatusCode, 0, "test", 0);
            mockResponse.Setup(x => x.EnsureSuccessStatusCode()).Throws(cosmosException);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemStreamAsync(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default))
                .ReturnsAsync(mockResponse.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("test failure", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_POCO_IdProvided_Status404_ReturnsSuccess()
        {
            object grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockResponse = new Mock<ResponseMessage>();
            mockResponse.Setup(x => x.IsSuccessStatusCode).Returns(false);
            var cosmosException = new CosmosException("test failure", HttpStatusCode.NotFound, 0, "test", 0);
            mockResponse.Setup(x => x.EnsureSuccessStatusCode()).Throws(cosmosException);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemStreamAsync(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default))
                .ReturnsAsync(mockResponse.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Null(conversionResult.Value);
        }

        [Fact]
        public async Task ConvertAsync_SinglePOCO_UsesSerializerFromBindingOptions()
        {
            // Arrange - mock serializer to verify it's called during deserialization
            var expected = new ToDoItem { Id = "1", Description = "Custom serializer result" };
            var mockSerializer = new Mock<ObjectSerializer>();
            mockSerializer
                .Setup(s => s.DeserializeAsync(It.IsAny<Stream>(), typeof(ToDoItem), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<object>(expected));

            var (converter, mockClient) = CreateConverterWithSerializer(mockSerializer.Object);

            var mockResponse = new Mock<ResponseMessage>();
            mockResponse.Setup(x => x.Content).Returns(new MemoryStream(Encoding.UTF8.GetBytes("{}")));

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemStreamAsync("1", It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);
            mockClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(
                GetTestBinaryData(id: "1", partitionKey: "1"), "CosmosDB");
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            // Act
            var result = await converter.ConvertAsync(context);

            // Assert - the custom serializer was invoked for deserialization
            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            Assert.Same(expected, result.Value);
            mockSerializer.Verify(
                s => s.DeserializeAsync(It.IsAny<Stream>(), typeof(ToDoItem), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ConvertAsync_CollectionPOCO_UsesSerializerFromBindingOptions()
        {
            // Arrange - mock serializer that delegates to a real JsonObjectSerializer
            var jsonSerializer = new JsonObjectSerializer(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var mockSerializer = new Mock<ObjectSerializer>();
            mockSerializer
                .Setup(s => s.DeserializeAsync(It.IsAny<Stream>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, Type type, CancellationToken ct) => jsonSerializer.DeserializeAsync(stream, type, ct));

            var (converter, mockClient) = CreateConverterWithSerializer(mockSerializer.Object);

            var json = @"{""Documents"":[{""Id"":""1"",""Description"":""Item 1""},{""Id"":""2"",""Description"":""Item 2""}],""_rid"":""test="",""_count"":2}";
            var mockFeedResponse = new Mock<ResponseMessage>();
            mockFeedResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
            mockFeedResponse.Setup(x => x.Content).Returns(new MemoryStream(Encoding.UTF8.GetBytes(json)));

            var mockFeedIterator = new Mock<FeedIterator>();
            mockFeedIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockFeedResponse.Object);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(), default, It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);
            mockClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var grpcModelBindingData = GrpcTestHelper.GetTestGrpcModelBindingData(
                GetTestBinaryData(query: "SELECT * FROM c"), "CosmosDB");
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            // Act
            var result = await converter.ConvertAsync(context);

            // Assert - the tracking serializer was used for collection deserialization
            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var items = Assert.IsAssignableFrom<IList<ToDoItem>>(result.Value);
            Assert.Equal(2, items.Count);
            Assert.Equal("Item 1", items[0].Description);
            mockSerializer.Verify(
                s => s.DeserializeAsync(It.IsAny<Stream>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private BinaryData GetTestBinaryData(string db = "testDb", string container = "testContainer", string connection = "cosmosConnection", string id = "", string partitionKey = "", string query = "", string location = "", string queryParams = "{}")
        {
            string jsonData = $@"{{
                                ""DatabaseName"" : ""{db}"",
                                ""ContainerName"" : ""{container}"",
                                ""Connection"" : ""{connection}"",
                                ""Id"" : ""{id}"",
                                ""PartitionKey"" : ""{partitionKey}"",
                                ""SqlQuery"" : ""{query}"",
                                ""PreferredLocations"" : ""{location}"",
                                ""SqlQueryParameters"" : {queryParams}
                            }}";

            return new BinaryData(jsonData);
        }

        private static (CosmosDBConverter converter, Mock<CosmosClient> mockClient) CreateConverterWithSerializer(
            ObjectSerializer serializer)
        {
            return CreateConverterWithExtensionOptions(new CosmosDBExtensionOptions { Serializer = serializer });
        }

        private static (CosmosDBConverter converter, Mock<CosmosClient> mockClient) CreateConverterWithExtensionOptions(
            CosmosDBExtensionOptions extensionOptions)
        {
            var mockClient = new Mock<CosmosClient>();

            var mockBindingOptions = new Mock<CosmosDBBindingOptions>();
            mockBindingOptions.Object.CosmosExtensionOptions = extensionOptions;
            mockBindingOptions
                .Setup(m => m.GetClient(It.IsAny<string>()))
                .Returns(mockClient.Object);

            var mockMonitor = new Mock<IOptionsMonitor<CosmosDBBindingOptions>>();
            mockMonitor
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockBindingOptions.Object);

            var converter = new CosmosDBConverter(mockMonitor.Object, new Mock<ILogger<CosmosDBConverter>>().Object);
            return (converter, mockClient);
        }

        public class ToDoItem
        {
            public string Id { get; set; }
            public string Description { get; set; }
        }
    }
}
