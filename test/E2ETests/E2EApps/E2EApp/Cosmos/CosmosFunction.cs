// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.E2EApp.Cosmos;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class CosmosFunction
    {
        [FunctionName("CosmosTrigger")]
        [CosmosDBOutput(
            name: "output",
            databaseName: "%CosmosDb%",
            collectionName: "%CosmosCollOut%",
            ConnectionStringSetting = "CosmosConnection",
            CreateIfNotExists = true)]
        public static void Run([CosmosDBTrigger(
            databaseName: "%CosmosDb%",
            collectionName: "%CosmosCollIn%",
            ConnectionStringSetting = "CosmosConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<MyDocument> input, FunctionContext context)
        {
            if (input != null && input.Count > 0)
            {
                foreach (var doc in input)
                {
                    context.Logger.LogInformation($"id: {doc.Id}");
                }

                context.OutputBindings["output"] = input.Select(p => new { id = p.Id });
            }
        }
    }
}
