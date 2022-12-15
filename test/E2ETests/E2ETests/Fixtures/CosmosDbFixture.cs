// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Tests.E2ETests;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.Azure.Functions.Worker.E2ETests.Fixtures
{

    public class CosmosDbFixture : FunctionAppFixture
    {
        private static readonly Uri inputCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName);
        private static readonly Uri outputCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName);
        private static readonly Uri leasesCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.LeaseCollectionName);


        public CosmosDbFixture(IMessageSink messageSink) : base(messageSink)
        {
        }

        private static Func<DocumentClient> CosmosClient() => () =>
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = Constants.CosmosDB.CosmosDBConnectionStringSetting
            };
            var serviceUri = new Uri(builder["AccountEndpoint"].ToString()!);

            return new DocumentClient(serviceUri, builder["AccountKey"].ToString());
        };

        // keep
        public async Task CreateDocument(string docId)
        {
            Document documentToTest = new Document()
            {
                Id = docId
            };

            var client = CosmosClient().Invoke();

            _ = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName), documentToTest);
        }

        // keep
        public async Task<string> ReadDocument(string docId)
        {
            var docUri = UriFactory.CreateDocumentUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName, docId);
            Document retrievedDocument = null;

            var client = CosmosClient().Invoke();
            await TestUtility.RetryAsync(async () =>
            {
                try
                {
                    retrievedDocument = await client.ReadDocumentAsync(docUri, new RequestOptions { PartitionKey = new PartitionKey(docId) });
                    return true;
                }
                catch (DocumentClientException ex) when (ex.Error.Code == "NotFound")
                {
                    return false;
                }
            }, pollingInterval: 500);

            return retrievedDocument?.Id;
        }

        // keep
        public async Task DeleteTestDocuments(string docId)
        {
            var inputDocUri = UriFactory.CreateDocumentUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName, docId);
            await DeleteDocument(inputDocUri);
            var outputDocUri = UriFactory.CreateDocumentUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName, docId);
            await DeleteDocument(outputDocUri);
        }


        private async Task DeleteDocument(Uri docUri)
        {
            try
            {
                var client = CosmosClient().Invoke();
                await client.DeleteDocumentAsync(docUri);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async Task<bool> CanConnectAsync(ILogger logger)
        {
            var client = CosmosClient().Invoke();
            try
            {
                await new HttpClient().GetAsync(client.ServiceEndpoint);
            }
            catch
            {
                // typically "the target machine actively refused it" if the emulator isn't running.
                logger.LogError($"Could not connect to CosmosDB endpoint: '{client.ServiceEndpoint}'. Are you using the emulator?");
                return false;
            }

            return true;
        }


        // keep
        public async Task<bool> TryCreateDocumentCollectionsAsync(ILogger logger)
        {

            var client = CosmosClient().Invoke();

            Database db = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = Constants.CosmosDB.DbName });
            Uri dbUri = UriFactory.CreateDatabaseUri(db.Id);

            await Task.WhenAll(
                CreateCollection(dbUri, Constants.CosmosDB.InputCollectionName),
                CreateCollection(dbUri, Constants.CosmosDB.OutputCollectionName),
                CreateCollection(dbUri, Constants.CosmosDB.LeaseCollectionName));

            return true;
        }

        public async Task DeleteDocumentCollections()
        {
            await Task.WhenAll(
                DeleteCollection(inputCollectionsUri),
                DeleteCollection(outputCollectionsUri),
                DeleteCollection(leasesCollectionsUri));
        }

        private async Task DeleteCollection(Uri collectionUri)
        {
            try
            {
                var client = CosmosClient().Invoke();
                await client.DeleteDocumentCollectionAsync(collectionUri);
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private async Task CreateCollection(Uri dbUri, string collectioName)
        {
            var pkd = new PartitionKeyDefinition();
            pkd.Paths.Add("/id");
            DocumentCollection collection = new DocumentCollection()
            {
                Id = collectioName,
                PartitionKey = pkd
            };
            var client = CosmosClient().Invoke();
            await client.CreateDocumentCollectionIfNotExistsAsync(dbUri, collection,
                new RequestOptions()
                {
                    PartitionKey = new PartitionKey("/id"),
                    OfferThroughput = 400
                });
        }

        #region Implementation of IAsyncLifetime

        public override async Task InitializeAsync()
        {
            await TryCreateDocumentCollectionsAsync(base.TestLogs);

            await base.InitializeAsync();
        }

        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();

            await DeleteDocumentCollections();
        }

        #endregion
    }

    [CollectionDefinition(Constants.CosmosFunctionAppCollectionName)]
    public class CosmosDbFixtureCollection : ICollectionFixture<CosmosDbFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
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


