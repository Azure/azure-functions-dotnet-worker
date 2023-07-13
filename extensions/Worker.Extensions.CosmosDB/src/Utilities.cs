// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

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
    }
}