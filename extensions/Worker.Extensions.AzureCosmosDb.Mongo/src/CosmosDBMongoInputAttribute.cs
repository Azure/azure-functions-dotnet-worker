// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to bind a parameter to documents read from an Azure Cosmos DB for MongoDB (vCore) collection.
    /// </summary>
    /// <remarks>
    /// The method parameter type can be one of the following:
    /// <list type="bullet">
    /// <item><description>A POCO type representing a single document.</description></item>
    /// <item><description><see cref="string"/> containing the JSON payload.</description></item>
    /// <item><description><see cref="System.Collections.Generic.IEnumerable{T}"/> or <see cref="System.Collections.Generic.List{T}"/> for query results.</description></item>
    /// <item><description><c>IMongoClient</c>, <c>IMongoDatabase</c>, or <c>IMongoCollection&lt;BsonDocument&gt;</c> from the MongoDB driver.</description></item>
    /// </list>
    /// </remarks>
    [InputConverter(typeof(CosmosDBMongoConverter))]
    [ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
    public sealed class CosmosDBMongoInputAttribute : InputBindingAttribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public CosmosDBMongoInputAttribute()
        {
            DatabaseName = string.Empty;
            CollectionName = string.Empty;
        }

        /// <summary>
        /// Constructs a new instance with the specified database name.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        public CosmosDBMongoInputAttribute(string databaseName)
        {
            DatabaseName = databaseName;
            CollectionName = string.Empty;
        }

        /// <summary>
        /// Constructs a new instance with the specified database and collection names.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        [JsonConstructor]
        public CosmosDBMongoInputAttribute(string databaseName, string collectionName)
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
        /// A MongoDB query expression to execute on the collection to produce results.
        /// May include binding parameters.
        /// </summary>
        public string? QueryString { get; set; }

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