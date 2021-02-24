// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class CosmosDBFunction
    {
        [Function("CosmosDBFunction")]
        [CosmosDBOutput("%CosmosDb%", "%CosmosCollOut%", ConnectionStringSetting = "CosmosConnection", CreateIfNotExists = true)]
        public static object Run(
            [CosmosDBTrigger("%CosmosDb%", "%CosmosCollIn%", ConnectionStringSetting = "CosmosConnection",
                LeaseCollectionName = "leases", CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<MyDocument> input,
            FunctionContext context)
        {
            var logger = context.GetLogger("CosmosDBFunction");

            if (input != null && input.Any())
            {
                foreach (var doc in input)
                {
                    logger.LogInformation($"Doc Id: {doc.Id}");
                }

                // Cosmos Output
                return input.Select(p => new { id = p.Id });
            }

            return null;
        }
    }

    public class MyDocument
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public int Number { get; set; }

        public bool Boolean { get; set; }
    }
}
