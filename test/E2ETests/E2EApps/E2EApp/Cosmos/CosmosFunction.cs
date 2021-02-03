using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.E2EApp.Cosmos;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class CosmosFunction
    {
        [FunctionName("CosmosTrigger")]
        public static void Run([CosmosDBTrigger(
            databaseName: "%CosmosDb%",
            collectionName: "%CosmosCollIn%",
            ConnectionStringSetting = "CosmosConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<MyDocument> input,
            [CosmosDB(
                databaseName: "%CosmosDb%",
                collectionName: "%CosmosCollOut%",
                ConnectionStringSetting = "CosmosConnection",
                CreateIfNotExists = true)] OutputBinding<IEnumerable<object>> output,
            FunctionExecutionContext context)
        {
            if (input != null && input.Count > 0)
            {
                foreach (var doc in input)
                {
                    context.Logger.LogInformation($"id: {doc.Id}");
                }

                output.SetValue(input.Select(p => new { id = p.Id }));
            }
        }
    }
}
