// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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

        [Fact(Skip = "Need to debug why Cosmos emulator does not always start.")]
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

        public void Dispose()
        {
            _disposeLog?.Dispose();
        }
    }
}
