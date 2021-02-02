// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public static class Constants
    {
        public static IConfiguration Configuration = TestUtility.GetTestConfiguration();
        public static string FunctionsHostUrl = Configuration["FunctionAppUrl"] ?? "http://localhost:7071";

        //Queue tests
        public static class Queue
        {
            public static string StorageConnectionStringSetting = Configuration["AzureWebJobsStorage"];
            public static string OutputBindingName = "test-output-node";
            public static string InputBindingName = "test-input-node";
        }

        // CosmosDB tests
        public static class CosmosDB
        {
            public static string CosmosDBConnectionStringSetting = Configuration["AzureWebJobsCosmosDBConnectionString"];
            public static string DbName = "ItemDb";
            public static string InputCollectionName = "ItemCollectionIn";
            public static string OutputCollectionName = "ItemCollectionOut";
            public static string LeaseCollectionName = "leases";
        }

        // EventHubs
        public static class EventHubs
        {
            public static string EventHubsConnectionStringSetting = Configuration["AzureWebJobsEventHubSender"];

            public static class Json_Test
            {
                public static string OutputName = "test-output-object-node";
                public static string InputName = "test-input-object-node";
            }

            public static class String_Test
            {
                public static string OutputName = "test-output-string-node";
                public static string InputName = "test-input-string-node";
            }

            public static class Cardinality_One_Test
            {
                public static string InputName = "test-input-one-node";
                public static string OutputName = "test-output-one-node";
            }
        }

        // Xunit Fixtures and Collections
        public const string FunctionAppCollectionName = "FunctionAppCollection";
    }
}