// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using DnsClient.Internal;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace FunctionApp
{
    public class MongoTriggerSimple
    {
        private readonly ILogger<MongoTriggerSimple> logger;

        public MongoTriggerSimple(ILogger<MongoTriggerSimple> logger)
        {
            this.logger = logger;
        }

        [Function(nameof(MongoTriggerSimple))]
        public void Run(
        [CosmosDBMongoTrigger("formio-dev-001",
            "submissions",
            ConnectionStringKey = "CosmosDB",
            LeaseCollectionName = "leases")] IEnumerable<BsonDocument> input)
        {
            foreach (BsonDocument doc in input)
            {
                logger.LogInformation("Doc triggered");
            }
        }
    }
}
