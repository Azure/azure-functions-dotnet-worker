// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Tests;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker.E2ETests
{
    public static class Constants
    {
        public static IConfiguration Configuration = TestUtility.GetTestConfiguration();
        public static string FunctionsHostUrl = Configuration["FunctionAppUrl"] ?? "http://localhost:7071";

        public const string StorageEmulatorConnectionString = "UseDevelopmentStorage=true";
        public static string StorageConnectionStringSetting = StorageEmulatorConnectionString;

        //Queue tests
        public static class Queue
        {
            public const string OutputBindingName = "test-output-dotnet-isolated";
            public const string InputBindingName = "test-input-dotnet-isolated";
            public const string InputArrayBindingName = "test-input-array-dotnet-isolated";
            public const string OutputArrayBindingName = "test-output-array-dotnet-isolated";
            public const string InputListBindingName = "test-input-list-dotnet-isolated";
            public const string OutputListBindingName = "test-output-list-dotnet-isolated";
            public const string InputBindingDataName = "test-input-binding-data-dotnet-isolated";
            public const string OutputBindingDataName = "test-output-binding-data-dotnet-isolated";
            public const string OutputBindingNamePOCO = "test-output-dotnet-isolated-poco";
            public const string InputBindingNamePOCO = "test-input-dotnet-isolated-poco";
            public const string InputBindingNameMetadata = "test-input-dotnet-isolated-metadata";
            public const string OutputBindingNameMetadata = "test-output-dotnet-isolated-metadata";
            public const string InputBindingNameQueueMessage = "test-input-dotnet-isolated-queuemessage";
            public const string OutputBindingNameQueueMessage = "test-output-dotnet-isolated-queuemessage";
            public const string InputBindingNameBinaryData = "test-input-dotnet-isolated-binarydata";
            public const string OutputBindingNameBinaryData = "test-output-dotnet-isolated-binarydata";
            public const string TestQueueMessage = "Hello, World";
        }

        //Blob tests
        public static class Blob
        {
            public const string TriggerInputBindingContainer = "test-trigger-dotnet-isolated";
            public const string InputBindingContainer = "test-input-dotnet-isolated";
            public const string OutputBindingContainer = "test-output-dotnet-isolated";
            public const string TriggerPocoContainer = "test-trigger-poco-dotnet-isolated";
            public const string OutputPocoContainer = "test-output-poco-dotnet-isolated";
            public const string TriggerStringContainer = "test-trigger-string-dotnet-isolated";
            public const string OutputStringContainer = "test-output-string-dotnet-isolated";
            public const string TriggerStreamContainer = "test-trigger-stream-dotnet-isolated";
            public const string OutputStreamContainer = "test-output-stream-dotnet-isolated";
            public const string TriggerBlobClientContainer = "test-trigger-blobclient-dotnet-isolated";
            public const string OutputBlobClientContainer = "test-output-blobclient-dotnet-isolated";
            public const string TriggerBlobContainerClientContainer = "test-trigger-containerclient-dotnet-isolated";
            public const string OutputBlobContainerClientContainer = "test-output-containerclient-dotnet-isolated";
        }

        // CosmosDB tests
        public static class CosmosDB
        {
            public const string EmulatorConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            public const string CosmosDBConnectionStringSetting = EmulatorConnectionString;
            public const string DbName = "ItemDb";
            public const string InputCollectionName = "ItemCollectionIn";
            public const string OutputCollectionName = "ItemCollectionOut";
            public const string LeaseCollectionName = "leases";
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

        // Tables tests
        public static class Tables
        {
            public const string EmulatorConnectionString = "UseDevelopmentStorage=true";
            public const string TablesConnectionStringSetting = EmulatorConnectionString;
            public const string TableName = "TestTable";
        }

        public static class TestAppNames
        {
            public const string E2EApp = "E2EApp";
            public const string E2EAspNetCoreApp = "E2EAspNetCoreApp";
        }
    }
}
