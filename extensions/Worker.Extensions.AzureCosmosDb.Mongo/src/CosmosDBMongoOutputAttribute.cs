// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to bind a parameter to documents written to an Azure Cosmos DB for MongoDB (vCore) collection.
    /// </summary>
    public sealed class CosmosDBMongoOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        public CosmosDBMongoOutputAttribute(string databaseName, string collectionName)
        {
            DatabaseName = databaseName;
            CollectionName = collectionName;
        }

        /// <summary>
        /// The name of the database to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// The name of the collection to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        public string CollectionName { get; private set; }

        /// <summary>
        /// Optional.
        /// The name of the app setting containing your Azure Cosmos DB for MongoDB connection string.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// Optional.
        /// If true, the database and collection will be automatically created if they do not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

        /// <summary>
        /// Optional.
        /// The Azure AD tenant ID for Microsoft Entra ID authentication.
        /// When specified, Entra ID authentication is used instead of native MongoDB authentication.
        /// May include binding parameters (e.g. "%TenantId%").
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Optional.
        /// The client ID for a user-assigned managed identity.
        /// Only used when <see cref="TenantId"/> is specified (Entra ID authentication).
        /// Leave empty to use a system-assigned managed identity.
        /// May include binding parameters (e.g. "%ManagedIdentityClientId%").
        /// </summary>
        public string? ManagedIdentityClientId { get; set; }
    }
}
