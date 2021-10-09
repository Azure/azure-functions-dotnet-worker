// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class CosmosFunction
    {
        [Function(nameof(CosmosTrigger))]
        [CosmosDBOutput(
            databaseName: "%CosmosDb%",
            containerName: "%CosmosCollOut%",
            Connection = "CosmosConnection",
            CreateIfNotExists = true)]
        public static object CosmosTrigger([CosmosDBTrigger(
            databaseName: "%CosmosDb%",
            containerName: "%CosmosCollIn%",
            Connection = "CosmosConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input, FunctionContext context)
        {
            if (input != null && input.Count > 0)
            {
                foreach (var doc in input)
                {
                    context.GetLogger("Function.CosmosTrigger").LogInformation($"id: {doc.Id}");
                }

                return input.Select(p => new { id = p.Id });
            }

            return null;
        }

        public class MyDocument
        {
            public string Id { get; set; }

            public string Text { get; set; }

            public int Number { get; set; }

            public bool Boolean { get; set; }
        }
    }
}
