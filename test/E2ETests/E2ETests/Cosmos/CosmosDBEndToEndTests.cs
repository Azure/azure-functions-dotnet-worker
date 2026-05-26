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

        // Verified passing 2026-05-21 against a real Cosmos account (continuous backup) with the
        // updated Worker.Extensions.CosmosDB / WebJobs.Extensions.CosmosDB packages. Re-skipped
        // because the Cosmos emulator does not reliably support AllVersionsAndDeletes change
        // feed mode — host indexing hangs ~36s on Account Read and returns 503 (Substatus 20003,
        // GatewayStoreClient Request Timeout). The trigger is also disabled by default in the
        // E2EApp's local.settings.json (AzureWebJobs.CosmosAllVersionsAndDeletesTrigger.Disabled)
        // so host startup stays clean against the emulator for the rest of the Cosmos test suite.
        // To run this test against a real Cosmos account: (1) flip Skip below, (2) point
        // CosmosConnection / CosmosDBConnectionStringSetting at the real account, (3) remove or
        // set the Disabled setting to "false".
        [Fact(Skip = "Emulator doesn't reliably support AllVersionsAndDeletes change feed mode.")]
        public async Task CosmosDBTrigger_AllVersionsAndDeletes_Succeeds()
        {
            string sourceDocId = Guid.NewGuid().ToString();
            string createdMarkerId = $"avad-Create-{sourceDocId}";
            string deletedMarkerId = $"avad-Delete-{sourceDocId}";

            try
            {
                // Create then delete a document in the AllVersionsAndDeletes-enabled container
                // to produce two change feed events.
                await CosmosDBHelpers.CreateAvadDocument(sourceDocId, sourceDocId);
                await CosmosDBHelpers.DeleteAvadDocument(sourceDocId);

                // The function writes one output document per change feed event, with the id
                // encoding the operation type. Wait for both to appear.
                string observedCreatedId = null;
                string observedDeletedId = null;
                await TestUtility.RetryAsync(async () =>
                {
                    observedCreatedId ??= await CosmosDBHelpers.ReadDocument(createdMarkerId);
                    observedDeletedId ??= await CosmosDBHelpers.ReadDocument(deletedMarkerId);
                    return observedCreatedId is not null && observedDeletedId is not null;
                });

                Assert.Equal(createdMarkerId, observedCreatedId);
                Assert.Equal(deletedMarkerId, observedDeletedId);
            }
            finally
            {
                // Best-effort cleanup of any docs written by the trigger and any leftover source doc.
                await CosmosDBHelpers.DeleteAvadDocument(sourceDocId);
                await CosmosDBHelpers.DeleteTestDocuments(createdMarkerId);
                await CosmosDBHelpers.DeleteTestDocuments(deletedMarkerId);
            }
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
