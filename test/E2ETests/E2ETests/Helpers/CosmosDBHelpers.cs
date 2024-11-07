// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public static class CosmosDBHelpers
    {
        private static readonly CosmosClient _cosmosClient;
        private static readonly Database _database;
        private static readonly Container _inputContainer;
        private static readonly Container _outputContainer;
        private static readonly Container _leasesContainer;

        static CosmosDBHelpers()
        {
            _cosmosClient = new CosmosClient(Constants.CosmosDB.CosmosDBConnectionStringSetting);
            _database = _cosmosClient.GetDatabase(Constants.CosmosDB.DbName);
            _inputContainer = _database.GetContainer(Constants.CosmosDB.InputCollectionName);
            _outputContainer = _database.GetContainer(Constants.CosmosDB.OutputCollectionName);
            _leasesContainer = _database.GetContainer(Constants.CosmosDB.LeaseCollectionName);
        }

        // keep
        public static async Task CreateDocument(string docId, string docText = "test")
        {
            await _inputContainer.CreateItemAsync(new Doc { Id = docId, Text = docText });
        }

        // keep
        public static async Task<string> ReadDocument(string docId)
        {
            Doc retrievedDocument = null;
            await TestUtility.RetryAsync(async () =>
            {
                try
                {
                    retrievedDocument = await _outputContainer.ReadItemAsync<Doc>(docId, new PartitionKey(docId));
                    return true;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

            }, pollingInterval: 500);

            return retrievedDocument?.Id;
        }

        // keep
        public static async Task DeleteTestDocuments(string docId)
        {
            await DeleteDocument(_inputContainer, docId);
            await DeleteDocument(_outputContainer, docId);
        }

        private static async Task DeleteDocument(Container container, string docId)
        {
            try
            {
                await container.DeleteItemAsync<Doc>(docId, new PartitionKey(docId));
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private static async Task<bool> CanConnectAsync(ILogger logger)
        {
            try
            {
                await new HttpClient().GetAsync(_cosmosClient.Endpoint);
            }
            catch
            {
                // typically "the target machine actively refused it" if the emulator isn't running.
                logger.LogError($"Could not connect to CosmosDB endpoint: '{_cosmosClient.Endpoint}'. Are you using the emulator?");
                return false;
            }

            return true;
        }


        // keep
        public static async Task<bool> TryCreateDocumentCollectionsAsync(ILogger logger)
        {
            if (!await CanConnectAsync(logger))
            {
                // This can hang if the service is unavailable. Just return and let tests fail.
                // The Cosmos tests may be filtered out anyway without an emulator running.
                return false;
            }

            await _cosmosClient.CreateDatabaseIfNotExistsAsync(Constants.CosmosDB.DbName);
            await _database.CreateContainerIfNotExistsAsync(Constants.CosmosDB.InputCollectionName, "/id");
            await _database.CreateContainerIfNotExistsAsync(Constants.CosmosDB.OutputCollectionName, "/id");
            await _database.CreateContainerIfNotExistsAsync(Constants.CosmosDB.LeaseCollectionName, "/id");

            return true;
        }

        public static async Task DeleteDocumentCollections()
        {
            await _inputContainer.DeleteContainerAsync().NoThrow();
            await _outputContainer.DeleteContainerAsync().NoThrow();
            await _leasesContainer.DeleteContainerAsync().NoThrow();
        }

        private static Task NoThrow(this Task t) => t.ContinueWith(_ => { });

        private class Doc
        {
            public string Id { get; set; }
            public string Text { get; set; }
        }
    }
}
