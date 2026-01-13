// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2ETests
{
    public static class CosmosDBHelpers
    {
        private static readonly CosmosClient _cosmosClient;
        private static readonly Container _inputContainer;
        private static readonly Container _outputContainer;
        private static readonly Container _leaseContainer;

        static CosmosDBHelpers()
        {
            _cosmosClient = new CosmosClient(Constants.CosmosDB.CosmosDBConnectionStringSetting);

            var database = _cosmosClient.GetDatabase(Constants.CosmosDB.DbName);
            _inputContainer = database.GetContainer(Constants.CosmosDB.InputCollectionName);
            _outputContainer = database.GetContainer(Constants.CosmosDB.OutputCollectionName);
            _leaseContainer = database.GetContainer(Constants.CosmosDB.LeaseCollectionName);
        }

        // keep
        public async static Task CreateDocument(string docId, string docText = "test")
        {
            var documentToTest = new { id = docId, Text = docText };
            await _inputContainer.CreateItemAsync(documentToTest, new PartitionKey(docId));
        }

        // keep
        public async static Task<string> ReadDocument(string docId)
        {
            try
            {
                var response = await _outputContainer.ReadItemAsync<dynamic>(docId, new PartitionKey(docId));
                return response.Resource?.id;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        // keep
        public async static Task DeleteTestDocuments(string docId)
        {
            await DeleteDocument(_inputContainer, docId);
            await DeleteDocument(_outputContainer, docId);
        }

        private async static Task DeleteDocument(Container container, string docId)
        {
            try
            {
                await container.DeleteItemAsync<dynamic>(docId, new PartitionKey(docId));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Ignore if the document is already deleted
            }
        }

        private async static Task<bool> CanConnectAsync(ILogger logger)
        {
            try
            {
                var response = await _cosmosClient.ReadAccountAsync();
                return response != null;
            }
            catch
            {
                logger.LogError($"Could not connect to CosmosDB. Check the emulator or connection string.");
                return false;
            }
        }

        // keep
        public async static Task<bool> TryCreateDocumentCollectionsAsync(ILogger logger)
        {
            if (!await CanConnectAsync(logger))
            {
                return false;
            }

            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(Constants.CosmosDB.DbName);
            var database = databaseResponse.Database;

            await Task.WhenAll(
                CreateCollection(database, Constants.CosmosDB.InputCollectionName),
                CreateCollection(database, Constants.CosmosDB.OutputCollectionName),
                CreateCollection(database, Constants.CosmosDB.LeaseCollectionName));

            return true;
        }

        public async static Task DeleteDocumentCollections()
        {
            var database = _cosmosClient.GetDatabase(Constants.CosmosDB.DbName);
            await Task.WhenAll(
                DeleteCollection(database, Constants.CosmosDB.InputCollectionName),
                DeleteCollection(database, Constants.CosmosDB.OutputCollectionName),
                DeleteCollection(database, Constants.CosmosDB.LeaseCollectionName));
        }

        private async static Task DeleteCollection(Database database, string collectionName)
        {
            try
            {
                var container = database.GetContainer(collectionName);
                await container.DeleteContainerAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Ignore if the container is already deleted
            }
        }

        private async static Task CreateCollection(Database database, string collectionName)
        {
            var containerProperties = new ContainerProperties
            {
                Id = collectionName,
                PartitionKeyPath = "/id"
            };

            await database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
        }
    }
}
