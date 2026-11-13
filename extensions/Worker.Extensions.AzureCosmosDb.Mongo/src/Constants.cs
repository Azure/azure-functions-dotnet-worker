// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo
{
    internal static class Constants
    {
        // Must match the host extension''s ParameterBindingData "Source" value exactly.
        internal const string MongoExtensionName = "AzureCosmosDBMongo";
        internal const string JsonContentType = "application/json";
        internal const string DefaultConnectionStringKey = "CosmosDBMongo";
        internal const string ApplicationName = "AzureCosmosDBMongoExtension";
    }
}