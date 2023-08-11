// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class CosmosDBFunction
    {
        private readonly ILogger<CosmosDBFunction> _logger;

        public CosmosDBFunction(ILogger<CosmosDBFunction> logger)
        {
            _logger = logger;
        }

        //<docsnippet_exponential_backoff_retry_example>
        [Function(nameof(CosmosDBFunction))]
        [ExponentialBackoffRetry(5, "00:00:04", "00:15:00")]
        [CosmosDBOutput("%CosmosDb%", "%CosmosContainerOut%", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
        public object Run(
            [CosmosDBTrigger(
                "%CosmosDb%",
                "%CosmosContainerIn%",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input,
            FunctionContext context)
        {
            if (input != null && input.Any())
            {
                foreach (var doc in input)
                {
                    _logger.LogInformation("Doc Id: {id}", doc.Id);
                }

                // Cosmos Output
                return input.Select(p => new { id = p.Id });
            }

            return null;
        }
        //</docsnippet_exponential_backoff_retry_example>
    }

    public class MyDocument
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public int Number { get; set; }

        public bool Boolean { get; set; }
    }
}
