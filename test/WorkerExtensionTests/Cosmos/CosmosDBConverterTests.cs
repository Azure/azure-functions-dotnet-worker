// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Cosmos;
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

namespace Microsoft.Azure.Functions.WorkerExtension.Tests
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
                .Setup(m => m.GetClient(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockCosmosClient.Object);

            var mockCosmosOptionsSnapshot = new Mock<IOptionsSnapshot<CosmosDBBindingOptions>>();
            mockCosmosOptionsSnapshot
                .Setup(m => m.Get(It.IsAny<string>()))
                .Returns(mockCosmosOptions.Object);

            _cosmosDBConverter = new CosmosDBConverter(mockCosmosOptionsSnapshot.Object, logger);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_CosmosClient_ReturnsSuccess()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
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
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(Database), grpcModelBindingData);

            var _mockDatabase = new Mock<Database>();
            _mockDatabase.Setup(m => m .Id).Returns("testId");

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
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(Container), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer.Setup(m => m .Id).Returns("testId");

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
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"));
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var expectedToDoItem = new ToDoItem() { Id = "1", Description = "Take out the rubbish"};

            var mockResponse = new Mock<ItemResponse<ToDoItem>>();
            mockResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            mockResponse.Setup(x => x.Resource).Returns(expectedToDoItem);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemAsync<ToDoItem>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, CancellationToken.None))
                .ReturnsAsync(mockResponse.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedToDoItem, conversionResult.Value);
        }

        [Fact]
        public async Task ConvertAsync_ValidModelBindingData_SinglePOCO_WithoutId_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(partitionKey: "1"));
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
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(id: "1"));
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
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(query: query, queryParams: queryParams));
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            var todo1 = new ToDoItem() { Id = "1", Description = "Take out the rubbish"};
            var todo2 = new ToDoItem() { Id = "2", Description = "Write unit tests for cosmos converter"};
            var expectedList = new List<ToDoItem>(){ todo1, todo2 };

            var mockFeedResponse = new Mock<FeedResponse<ToDoItem>>();
            mockFeedResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            mockFeedResponse.Setup(x => x.Resource).Returns(expectedList);

            var mockFeedIterator = new Mock<FeedIterator<ToDoItem>>();
            mockFeedIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            mockFeedIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(mockFeedResponse.Object);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.GetItemQueryIterator<ToDoItem>(It.IsAny<QueryDefinition>(), default, It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expectedList, conversionResult.Value);
        }

        [Fact]
        public async Task ConvertAsync_Container_NullFeedIterator_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer.Setup(m => m .Id).Returns("testId");
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
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(query: query, queryParams: queryParams));
            var context = new TestConverterContext(typeof(IEnumerable<ToDoItem>), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer.Setup(m => m .Id).Returns("testId");
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

        [Fact] // Should we fail if the result is ever null?
        public async Task ConvertAsync_ResultIsNull_ReturnsUnhandled()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(Database), grpcModelBindingData);

            _mockCosmosClient
                .Setup(m => m.GetDatabase(It.IsAny<string>()))
                .Returns<Database>(null);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ThrowsException_ReturnsFailure()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData());
            var context = new TestConverterContext(typeof(Database), grpcModelBindingData);

            _mockCosmosClient
                .Setup(m => m.GetDatabase(It.IsAny<string>()))
                .Throws(new Exception());

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ItemResponse_ResourceIsNull_ThrowsException_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"));
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockResponse = new Mock<ItemResponse<ToDoItem>>();
            mockResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            mockResponse.Setup(x => x.Resource).Returns((ToDoItem)null);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemAsync<ToDoItem>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, CancellationToken.None))
                .ReturnsAsync(mockResponse.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unable to retrieve document with ID '1' and PartitionKey '1'", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataSource_NotCosmosExtension_ReturnsUnhandled()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(), source: "anotherExtensions");
            var context = new TestConverterContext(typeof(CosmosClient), grpcModelBindingData);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_ModelBindingDataContentType_Unsupported_ReturnsFailed()
        {
            var grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(), contentType: "binary");
            var context = new TestConverterContext(typeof(CosmosClient), grpcModelBindingData);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unexpected content-type. Currently only 'application/json' is supported.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_CosmosContainerIsNull_ThrowsException_ReturnsFailure()
        {
            object grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(container: "myContainer"));
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<Container>(null);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal($"Unable to create Cosmos container client for 'myContainer'.", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_POCO_ItemResponseNull_ThrowsException_ReturnsFailure()
        {
            object grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"));
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemAsync<ToDoItem>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, CancellationToken.None))
                .Returns(Task.FromResult<ItemResponse<ToDoItem>>(null));

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unable to retrieve document with ID '1' and PartitionKey '1'", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_POCO_IdProvided_StatusNot200_ThrowsException_ReturnsFailure()
        {
            object grpcModelBindingData = GetTestGrpcModelBindingData(GetTestBinaryData(id: "1", partitionKey: "1"));
            var context = new TestConverterContext(typeof(ToDoItem), grpcModelBindingData);

            var mockResponse = new Mock<ItemResponse<ToDoItem>>();
            mockResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.InternalServerError);

            var mockContainer = new Mock<Container>();
            mockContainer
                .Setup(m => m.ReadItemAsync<ToDoItem>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, CancellationToken.None))
                .ReturnsAsync(mockResponse.Object);

            _mockCosmosClient
                .Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            var conversionResult = await _cosmosDBConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Unable to retrieve document with ID '1' and PartitionKey '1'", conversionResult.Error.Message);
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

        private GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData content, string source = "CosmosDB", string contentType = "application/json")
        {
            var data = new ModelBindingData()
            {
                Version = "1.0",
                Source = source,
                Content = ByteString.CopyFrom(content),
                ContentType = contentType
            };

            return new GrpcModelBindingData(data);
        }

        public class ToDoItem
        {
            public string Id { get; set; }
            public string Description { get; set; }
        }
    }
}