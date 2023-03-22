// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests.Cosmos
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class CosmosDBEndToEndTests : IDisposable
    {
        private readonly IDisposable _disposeLog;
        private readonly FunctionAppFixture _fixture;

        public CosmosDBEndToEndTests(FunctionAppFixture fixture, ITestOutputHelper testOutput)
        {
            _fixture = fixture;
            _disposeLog = _fixture.TestLogs.UseTestLogger(testOutput);
        }

        [Fact]
        public async Task CosmosDBTriggerAndOutput_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            try
            {
                //Trigger
                await CosmosDBHelpers.CreateDocument(expectedDocId);

                //Read
                var documentId = await CosmosDBHelpers.ReadDocument(expectedDocId);
                Assert.Equal(expectedDocId, documentId);
            }
            finally
            {
                //Clean up
                await CosmosDBHelpers.DeleteTestDocuments(expectedDocId);
            }
        }

        [Theory]
        [InlineData("DocsByUsingCosmosClient")]
        [InlineData("DocsByUsingDatabaseClient")]
        [InlineData("DocsByUsingContainerClient")]
        public async Task CosmosInput_ClientBinding_Succeeds(string functionName)
        {
            string expectedDocId = Guid.NewGuid().ToString();
            try
            {
                //Setup
                await CosmosDBHelpers.CreateDocument(expectedDocId);

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains(expectedDocId, actualMessage);
            }
            finally
            {
                //Clean up
                await CosmosDBHelpers.DeleteTestDocuments(expectedDocId);
            }
        }

        [Fact]
        public async Task CosmosInput_DocByIdFromRouteData_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            string functionPath = $"docsbyroute/{expectedDocId}/{expectedDocId}";
            try
            {
                //Setup
                MyDocument document = new() { Id = expectedDocId, Text = "hello world", Number = 1, Boolean = true };
                await CosmosDBHelpers.CreateDocument(document);

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionPath);
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains("hello world", actualMessage);
            }
            finally
            {
                //Clean up
                await CosmosDBHelpers.DeleteTestDocuments(expectedDocId);
            }
        }

        [Fact]
        public async Task CosmosInput_DocByIdFromRouteDataUsingSqlQuery_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            string functionPath = $"docsbysql/{expectedDocId}";
            try
            {
                //Setup
                MyDocument document = new() { Id = expectedDocId, Text = "hello world", Number = 1, Boolean = true };
                await CosmosDBHelpers.CreateDocument(document);

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionPath);
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains("hello world", actualMessage);
            }
            finally
            {
                //Clean up
                await CosmosDBHelpers.DeleteTestDocuments(expectedDocId);
            }
        }

        [Fact]
        public async Task CosmosInput_DocByIdFromQueryStringUsingSqlQuery_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            string functionName = "DocByIdFromQueryStringUsingSqlQuery";
            string requestBody = @$"{{ ""id"": ""{expectedDocId}"" }}";
            try
            {
                //Setup
                MyDocument document = new() { Id = expectedDocId, Text = "hello world", Number = 1, Boolean = true };
                await CosmosDBHelpers.CreateDocument(document);

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody(functionName, requestBody, "application/json");
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains("hello world", actualMessage);
            }
            finally
            {
                //Clean up
                await CosmosDBHelpers.DeleteTestDocuments(expectedDocId);
            }
        }

        public void Dispose()
        {
            _disposeLog?.Dispose();
        }

        public class MyDocument
        {
            public string Id { get; set; }

            public string Text { get; set; }

            public int Number { get; set; }

            public bool Boolean { get; set; }
        }
    }
}
