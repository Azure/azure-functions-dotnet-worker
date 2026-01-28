// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.E2ETests.Cosmos
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
                await CosmosDBHelpers.CreateDocument(expectedDocId, expectedDocId);

                //Read
                string documentId = string.Empty;
                await TestUtility.RetryAsync(async () =>
                {
                    documentId = await CosmosDBHelpers.ReadDocument(expectedDocId);
                    return documentId is not null;
                });

                //Assert
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
                await CosmosDBHelpers.CreateDocument(expectedDocId, expectedDocId);

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
                await CosmosDBHelpers.CreateDocument(expectedDocId, "DocByIdFromRouteData");

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionPath);
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains("DocByIdFromRouteData", actualMessage);
            }
            finally
            {
                //Clean up
                await CosmosDBHelpers.DeleteTestDocuments(expectedDocId);
            }
        }

        [Fact]
        public async Task CosmosInput_DocByIdFromRouteDataNotFound_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            string functionPath = $"docsbyroute/{expectedDocId}/{expectedDocId}";
            try
            {
                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionPath);

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.NotFound;

                Assert.Equal(expectedStatusCode, response.StatusCode);
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
                await CosmosDBHelpers.CreateDocument(expectedDocId, "DocByIdFromRouteDataUsingSqlQuery");

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionPath);
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains("DocByIdFromRouteDataUsingSqlQuery", actualMessage);
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
                await CosmosDBHelpers.CreateDocument(expectedDocId, functionName);

                //Trigger
                HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody(functionName, requestBody, "application/json");
                string actualMessage = await response.Content.ReadAsStringAsync();

                //Verify
                HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

                Assert.Equal(expectedStatusCode, response.StatusCode);
                Assert.Contains(functionName, actualMessage);
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
    }
}
