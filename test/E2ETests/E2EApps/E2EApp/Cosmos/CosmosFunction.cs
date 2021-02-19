// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
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
                    context.GetLogger("Function.CosmosTrigger").LogInformation($"id: {doc.Id}");
                }

                context.OutputBindings["output"] = input.Select(p => new { id = p.Id });
            }
        }

        public class MyDocument
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("number")]
            public int Number { get; set; }

            [JsonPropertyName("boolean")]
            public bool Boolean { get; set; }
        }
    }
}
