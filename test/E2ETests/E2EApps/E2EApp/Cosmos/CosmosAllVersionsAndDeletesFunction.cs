// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public class CosmosAllVersionsAndDeletesFunction
    {
        // The output document id encodes the change feed operation so the E2E test
        // can verify the trigger fired and observed the expected operation type.
        // For Delete events, Current is null and Previous carries the document.
        [Function(nameof(CosmosAllVersionsAndDeletesTrigger))]
        [CosmosDBOutput(
            databaseName: "%CosmosDb%",
            containerName: "%CosmosCollOut%",
            Connection = "CosmosConnection",
            CreateIfNotExists = true)]
        public IEnumerable<object> CosmosAllVersionsAndDeletesTrigger(
            [CosmosDBTrigger(
                databaseName: "%CosmosDb%",
                containerName: "%CosmosCollAvad%",
                Connection = "CosmosConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "avad",
                CreateLeaseContainerIfNotExists = true,
                ChangeFeedMode = CosmosDBChangeFeedMode.AllVersionsAndDeletes)] IReadOnlyList<ChangeFeedItem<AvadDocument>> input,
                FunctionContext context)
        {
            if (input == null || input.Count == 0)
            {
                return null;
            }

            ILogger logger = context.GetLogger("Function.CosmosAllVersionsAndDeletesTrigger");

            return input.Select(change =>
            {
                string operation = change.Metadata?.OperationType.ToString() ?? "Unknown";
                // Prefer Current/Previous payload id; fall back to Metadata.Id which is populated
                // even for Delete events on accounts where Previous is not retained
                // (e.g. continuous backup mode).
                string sourceId = change.Current?.Id ?? change.Previous?.Id ?? change.Metadata?.Id;
                logger.LogInformation("AllVersionsAndDeletes change: operation={Operation}, id={Id}", operation, sourceId);

                return new
                {
                    id = $"avad-{operation}-{sourceId}"
                };
            }).ToArray();
        }

        public class AvadDocument
        {
            public string Id { get; set; }

            public string Text { get; set; }
        }
    }
}
