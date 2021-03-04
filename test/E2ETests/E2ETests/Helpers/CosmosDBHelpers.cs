// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public static class CosmosDBHelpers
    {
        private static readonly DocumentClient _docDbClient;
        private static readonly Uri inputCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName);
        private static readonly Uri outputCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName);
        private static readonly Uri leasesCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.LeaseCollectionName);

        static CosmosDBHelpers()
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = Constants.CosmosDB.CosmosDBConnectionStringSetting
            };
            var serviceUri = new Uri(builder["AccountEndpoint"].ToString());
            _docDbClient = new DocumentClient(serviceUri, builder["AccountKey"].ToString());
        }

        // keep
        public async static Task CreateDocument(string docId)
        {
            Document documentToTest = new Document()
            {
                Id = docId
            };

            _ = await _docDbClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName), documentToTest);
        }

        // keep
        public async static Task<string> ReadDocument(string docId)
        {
            var docUri = UriFactory.CreateDocumentUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName, docId);
            Document retrievedDocument = null;
            await TestUtility.RetryAsync(async () =>
            {
                try
                {
                    retrievedDocument = await _docDbClient.ReadDocumentAsync(docUri, new RequestOptions { PartitionKey = new PartitionKey(docId) });
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
        public async static Task DeleteTestDocuments(string docId)
        {
            var inputDocUri = UriFactory.CreateDocumentUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName, docId);
            await DeleteDocument(inputDocUri);
            var outputDocUri = UriFactory.CreateDocumentUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName, docId);
            await DeleteDocument(outputDocUri);
        }

        private async static Task DeleteDocument(Uri docUri)
        {
            try
            {
                await _docDbClient.DeleteDocumentAsync(docUri);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async static Task<bool> CanConnectAsync(ILogger logger)
        {
            try
            {
                await new HttpClient().GetAsync(_docDbClient.ServiceEndpoint);
            }
            catch
            {
                // typically "the target machine actively refused it" if the emulator isn't running.
                logger.LogError($"Could not connect to CosmosDB endpoint: '{_docDbClient.ServiceEndpoint}'. Are you using the emulator?");
                return false;
            }

            return true;
        }


        // keep
        public async static Task<bool> TryCreateDocumentCollectionsAsync(ILogger logger)
        {
            if (!await CanConnectAsync(logger))
            {
                // This can hang if the service is unavailable. Just return and let tests fail.
                // The Cosmos tests may be filtered out anyway without an emulator running.
                return false;
            }

            Database db = await _docDbClient.CreateDatabaseIfNotExistsAsync(new Database { Id = Constants.CosmosDB.DbName });
            Uri dbUri = UriFactory.CreateDatabaseUri(db.Id);

            await Task.WhenAll(
                CreateCollection(dbUri, Constants.CosmosDB.InputCollectionName),
                CreateCollection(dbUri, Constants.CosmosDB.OutputCollectionName),
                CreateCollection(dbUri, Constants.CosmosDB.LeaseCollectionName));

            return true;
        }

        public async static Task DeleteDocumentCollections()
        {
            await Task.WhenAll(
                DeleteCollection(inputCollectionsUri),
                DeleteCollection(outputCollectionsUri),
                DeleteCollection(leasesCollectionsUri));
        }

        private async static Task DeleteCollection(Uri collectionUri)
        {
            try
            {
                await _docDbClient.DeleteDocumentCollectionAsync(collectionUri);
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private async static Task CreateCollection(Uri dbUri, string collectioName)
        {
            var pkd = new PartitionKeyDefinition();
            pkd.Paths.Add("/id");
            DocumentCollection collection = new DocumentCollection()
            {
                Id = collectioName,
                PartitionKey = pkd
            };
            await _docDbClient.CreateDocumentCollectionIfNotExistsAsync(dbUri, collection,
                new RequestOptions()
                {
                    PartitionKey = new PartitionKey("/id"),
                    OfferThroughput = 400
                });
        }
    }
}
