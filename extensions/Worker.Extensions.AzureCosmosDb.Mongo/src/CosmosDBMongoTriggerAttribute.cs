// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to define a trigger that fires when changes occur on a monitored
    /// Azure Cosmos DB for MongoDB (vCore) collection, database, or cluster.
    /// </summary>
    /// <remarks>
    /// Leave <paramref name="databaseName"/> empty to monitor at the cluster level, or provide a
    /// database name while leaving the collection name empty to monitor at the database level.
    /// </remarks>
    public sealed class CosmosDBMongoTriggerAttribute : TriggerBindingAttribute
    {
        /// <summary>
        /// Triggers an event when changes occur on the monitored target.
        /// </summary>
        /// <param name="databaseName">Name of the database to monitor for changes.</param>
        /// <param name="collectionName">Name of the collection to monitor for changes.</param>
        public CosmosDBMongoTriggerAttribute(string databaseName, string collectionName)
        {
            DatabaseName = databaseName;
            CollectionName = collectionName;
        }

        /// <summary>
        /// Name of the database to monitor for changes.
        /// May include binding parameters.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Name of the collection to monitor for changes.
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
        /// If true, the monitored database and collection will be automatically created if they do not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

        /// <summary>
        /// Optional.
        /// Name of the database containing the lease collection.
        /// If not specified, defaults to the monitored database name.
        /// </summary>
        public string? LeaseDatabaseName { get; set; }

        /// <summary>
        /// Optional.
        /// Name of the lease collection. If not specified, defaults to "leases".
        /// </summary>
        public string? LeaseCollectionName { get; set; }

        /// <summary>
        /// Optional.
        /// The name of the app setting containing the connection string for the lease cluster.
        /// If not specified, defaults to the monitored cluster connection string.
        /// </summary>
        public string? LeaseConnectionStringSetting { get; set; }

        /// <summary>
        /// Optional.
        /// The Azure AD tenant ID for Microsoft Entra ID authentication for the monitored cluster.
        /// When specified, Entra ID authentication is used instead of native MongoDB authentication.
        /// May include binding parameters (e.g. "%TenantId%").
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Optional.
        /// The client ID for a user-assigned managed identity for the monitored cluster.
        /// Only used when <see cref="TenantId"/> is specified (Entra ID authentication).
        /// Leave empty to use a system-assigned managed identity.
        /// May include binding parameters (e.g. "%ManagedIdentityClientId%").
        /// </summary>
        public string? ManagedIdentityClientId { get; set; }

        /// <summary>
        /// Optional.
        /// The Azure AD tenant ID for Microsoft Entra ID authentication for the lease cluster.
        /// May include binding parameters (e.g. "%LeaseTenantId%").
        /// </summary>
        public string? LeaseTenantId { get; set; }

        /// <summary>
        /// Optional.
        /// The client ID for a user-assigned managed identity for the lease cluster.
        /// May include binding parameters (e.g. "%LeaseManagedIdentityClientId%").
        /// </summary>
        public string? LeaseManagedIdentityClientId { get; set; }
    }
}
