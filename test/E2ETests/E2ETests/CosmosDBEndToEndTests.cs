// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.E2ETests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    [Collection(Constants.CosmosFunctionAppCollectionName)]
    public class CosmosDBEndToEndTests
    {
        private const int SECONDS = 1000;
        private const int DBTIMEOUT = 15 * SECONDS;

        private readonly CosmosDbFixture _fixture;

        public CosmosDBEndToEndTests(CosmosDbFixture fixture, ITestOutputHelper testOutput)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CosmosDBTriggerAndOutput_Succeeds()
        {
            string expectedDocId = Guid.NewGuid().ToString();
            try
            {
                //Trigger
                await _fixture.CreateDocument(expectedDocId);

                //Read
                var documentId = await _fixture.ReadDocument(expectedDocId);
                Assert.Equal(expectedDocId, documentId);

                await TestUtility.RetryAsync(() => { 
                    var _ = _fixture.TestLogs.CoreToolsLogs.Any(x => x.Contains("Executed 'Functions.CosmosTrigger'"));
                    return Task.FromResult(_);
                },
                timeout: DBTIMEOUT,
                userMessageCallback: () => $"Trigger log was not found"
                );

            }
            finally
            {
                //Clean up
                await _fixture.DeleteTestDocuments(expectedDocId);
            }
        }
    }
}
