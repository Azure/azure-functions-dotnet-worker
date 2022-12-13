// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{

    public class CosmosDbFixture : IDisposable, IAsyncLifetime
    {
        private readonly CosmosClient _docDbClient;

        public CosmosDbFixture()
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = Constants.CosmosDB.CosmosDBConnectionStringSetting
            };
            var serviceUri = new Uri(builder["AccountEndpoint"].ToString()!);

            _docDbClient = new CosmosClient(serviceUri.ToString(), builder["AccountKey"].ToString());
        }

        // keep
        public async Task CreateDocument(string docId, string collection = Constants.CosmosDB.InputCollectionName)
        {

            var container = _docDbClient.GetContainer(Constants.CosmosDB.DbName, collection);

            _ = await container.CreateItemAsync(new Item
            {
                id = docId
            }, new PartitionKey(docId));
        }

        // keep
        public async Task<string> ReadDocument(string docId, string collection = Constants.CosmosDB.InputCollectionName)
        {
            var container = _docDbClient.GetContainer(Constants.CosmosDB.DbName, collection);

            Item retrievedDocument = null;
            await TestUtility.RetryAsync(async () =>
            {
                retrievedDocument = await container.ReadItemAsync<Item>(docId, new PartitionKey(docId));
                return true;
            }, pollingInterval: 500);


            return retrievedDocument?.id;
        }

        // keep
        public async Task DeleteTestDocuments(string docId)
        {

            await DeleteDocument(docId, Constants.CosmosDB.InputCollectionName);

            await DeleteDocument(docId, Constants.CosmosDB.OutputCollectionName);
        }

        private async Task DeleteDocument(string docId, string collection)
        {


            try
            {
                var container = _docDbClient.GetContainer(Constants.CosmosDB.DbName, collection);

                await container.DeleteItemAsync<Item>(docId, new PartitionKey(docId));
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async Task<bool> CanConnectAsync(ILogger logger)
        {
            try
            {
                await new HttpClient().GetAsync(_docDbClient.Endpoint);
            }
            catch
            {
                // typically "the target machine actively refused it" if the emulator isn't running.
                logger.LogError($"Could not connect to CosmosDB endpoint: '{_docDbClient.Endpoint}'. Are you using the emulator?");
                return false;
            }

            return true;
        }


        // keep
        public async Task<bool> TryCreateDocumentCollectionsAsync(ILogger logger)
        {
            if (!await CanConnectAsync(logger))
            {
                // This can hang if the service is unavailable. Just return and let tests fail.
                // The Cosmos tests may be filtered out anyway without an emulator running.
                return false;
            }

            await CreateCollection(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName);
            await CreateCollection(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName);
            await CreateCollection(Constants.CosmosDB.DbName, Constants.CosmosDB.LeaseCollectionName);

            return true;
        }

        public async Task DeleteDocumentCollections()
        {
            await DeleteCollection(Constants.CosmosDB.InputCollectionName);
            await DeleteCollection(Constants.CosmosDB.OutputCollectionName);
            await DeleteCollection(Constants.CosmosDB.LeaseCollectionName);
        }

        private async Task DeleteCollection(string collection)
        {
            try
            {
                var container = _docDbClient.GetContainer(Constants.CosmosDB.DbName, collection);

                await container.DeleteContainerAsync();
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private async Task CreateCollection(string dbId, string collectionName)
        {

            var response = await _docDbClient.CreateDatabaseIfNotExistsAsync(dbId);
            var db = response.Database;

            await db.CreateContainerIfNotExistsAsync(collectionName, "/id");


        }

        #region Implementation of IDisposable

        public void Dispose()
        {
        }

        #endregion

        #region Implementation of IAsyncLifetime

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        #endregion
    }


    public class Item
    {
        public Item()
        {
            id = Guid.NewGuid().ToString("N");
        }

        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public string id { get; set; }


        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isComplete")]
        public bool Completed { get; set; }
    }

}


