// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;

[assembly: WorkerExtensionStartup(typeof(CosmosDBMongoExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Startup class for the Azure Cosmos DB for MongoDB (vCore) worker extension.
    /// </summary>
    public class CosmosDBMongoExtensionStartup : WorkerExtensionStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            applicationBuilder.ConfigureCosmosDBMongoExtension();
        }
    }
}