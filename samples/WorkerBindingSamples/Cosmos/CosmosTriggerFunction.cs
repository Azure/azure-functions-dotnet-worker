// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Samples demonstrating binding to the <see cref="IReadOnlyList{T}"/> type.
    /// </summary>
    public class CosmosTriggerFunction
    {
        private readonly ILogger<CosmosTriggerFunction> _logger;

        public CosmosTriggerFunction(ILogger<CosmosTriggerFunction> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This function demonstrates binding to a collection of <see cref="ToDoItem"/>.
        /// </summary>
        [Function(nameof(CosmosTriggerFunction))]
        public void Run([CosmosDBTrigger(
            databaseName: "ToDoItems",
            containerName:"TriggerItems",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<ToDoItem> todoItems,
            FunctionContext context)
        {
            if (todoItems is not null && todoItems.Any())
            {
                foreach (var doc in todoItems)
                {
                    _logger.LogInformation("ToDoItem: {desc}", doc.Description);
                }
            }
        }
    }
}
