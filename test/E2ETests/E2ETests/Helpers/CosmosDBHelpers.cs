// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public static class CosmosDBHelpers
    {
        private static DocumentClient _docDbClient;
        private static Uri inputCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.InputCollectionName);
        private static Uri outputCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.OutputCollectionName);
        private static Uri leasesCollectionsUri = UriFactory.CreateDocumentCollectionUri(Constants.CosmosDB.DbName, Constants.CosmosDB.LeaseCollectionName);

        static CosmosDBHelpers()
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder();
            builder.ConnectionString = Constants.CosmosDB.CosmosDBConnectionStringSetting;
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

        // keep
        public async static Task CreateDocumentCollections()
        {
            Database db = await _docDbClient.CreateDatabaseIfNotExistsAsync(new Database { Id = Constants.CosmosDB.DbName });
            Uri dbUri = UriFactory.CreateDatabaseUri(db.Id);

            Console.WriteLine(_docDbClient.ServiceEndpoint);

            await CreateCollection(dbUri, Constants.CosmosDB.InputCollectionName);
            await CreateCollection(dbUri, Constants.CosmosDB.OutputCollectionName);
            await CreateCollection(dbUri, Constants.CosmosDB.LeaseCollectionName);

            var q = _docDbClient.CreateDocumentCollectionQuery(db.SelfLink);
            foreach (var c in q)
            {
                Console.WriteLine(c.Id);
                Console.WriteLine(c.SelfLink);
            }
        }
        public async static Task DeleteDocumentCollections()
        {
            await DeleteCollection(inputCollectionsUri);
            await DeleteCollection(outputCollectionsUri);
            await DeleteCollection(leasesCollectionsUri);
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
