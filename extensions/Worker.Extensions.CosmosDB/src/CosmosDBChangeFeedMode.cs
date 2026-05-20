// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents the change feed mode for the Cosmos DB Trigger.
    /// </summary>
    public enum CosmosDBChangeFeedMode
    {
        /// <summary>
        /// Only the latest version of each item is included. Deletes are not surfaced.
        /// </summary>
        LatestVersion = 0,

        /// <summary>
        /// All intermediate versions and delete tombstones are included.
        /// </summary>
        AllVersionsAndDeletes = 1
    }
}
