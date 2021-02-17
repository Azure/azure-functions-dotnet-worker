// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class CosmosDBFunction
    {
        [FunctionName("CosmosDBFunction")]
        [CosmosDBOutput("output", "%CosmosDb%", "%CosmosCollOut%", ConnectionStringSetting = "CosmosConnection", CreateIfNotExists = true)]
        public static void Run(
            [CosmosDBTrigger("%CosmosDb%", "%CosmosCollIn%", ConnectionStringSetting = "CosmosConnection",
                LeaseCollectionName = "leases", CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<MyDocument> input,
            FunctionExecutionContext context)
        {
            var logger = context.Logger;

            if (input != null && input.Any())
            {
                foreach (var doc in input)
                {
                    logger.LogInformation($"Doc Id: {doc.Id}");
                }

                // Cosmos Output
                context.OutputBindings["output"] = input.Select(p => new { id = p.Id });
            }
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
