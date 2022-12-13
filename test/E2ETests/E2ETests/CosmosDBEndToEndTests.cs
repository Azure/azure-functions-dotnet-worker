// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class CosmosDbEndToEndTests : IClassFixture<CosmosDbFixture>, IAsyncLifetime
    {
        private readonly FunctionAppFixture _fixture;
        private readonly CosmosDbFixture _cosmosDbFixture;
        private readonly IDisposable _disposeLog;

        public CosmosDbEndToEndTests(FunctionAppFixture fixture, CosmosDbFixture cosmosDbFixture , ITestOutputHelper testOutput)
        {
            _fixture = fixture;
            _cosmosDbFixture = cosmosDbFixture;
            _disposeLog = fixture.TestLogs.UseTestLogger(testOutput);
        }

        [Fact]
        public async Task CosmosDBTriggerAndOutput_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            try
            {
                //Trigger
                await _cosmosDbFixture.CreateDocument(expectedDocId);

                //Read
                var documentId = await _cosmosDbFixture.ReadDocument(expectedDocId);
                Assert.Equal(expectedDocId, documentId);
            }
            finally
            {
                //Clean up
                await _cosmosDbFixture.DeleteTestDocuments(expectedDocId);
            }
        }

        #region Implementation of IAsyncLifetime

        public async Task InitializeAsync()
        {
            await _cosmosDbFixture.TryCreateDocumentCollectionsAsync(_fixture.TestLogs);
        }

        public async Task DisposeAsync()
        {
            await _cosmosDbFixture.DeleteDocumentCollections();

            _disposeLog?.Dispose();
        }

        #endregion
    }
}
