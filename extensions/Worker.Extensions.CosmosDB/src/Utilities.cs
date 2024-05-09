// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker.Extensions.CosmosDB
{
    internal class Utilities
    {
        internal static IReadOnlyList<string> ParsePreferredLocations(string preferredRegions)
        {
            if (string.IsNullOrEmpty(preferredRegions))
            {
                return Enumerable.Empty<string>().ToList();
            }

            return preferredRegions
                .Split(',')
                .Select((region) => region.Trim())
                .Where((region) => !string.IsNullOrEmpty(region))
                .ToList();
        }

        internal static CosmosClientOptions BuildClientOptions(ConnectionMode? connectionMode, CosmosSerializer serializer, string preferredLocations, string userAgent)
        {
            CosmosClientOptions cosmosClientOptions = new ();

            if (connectionMode.HasValue)
            {
                cosmosClientOptions.ConnectionMode = connectionMode.Value;
            }

            if (!string.IsNullOrEmpty(preferredLocations))
            {
                cosmosClientOptions.ApplicationPreferredRegions = ParsePreferredLocations(preferredLocations);
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                cosmosClientOptions.ApplicationName = userAgent;
            }

            if (serializer != null)
            {
                cosmosClientOptions.Serializer = serializer;
            }

            return cosmosClientOptions;
        }
    }
}