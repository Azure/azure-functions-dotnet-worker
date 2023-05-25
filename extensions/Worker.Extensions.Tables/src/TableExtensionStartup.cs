// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: WorkerExtensionStartup(typeof(TableExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Table extension startup.
    /// </summary>
    public class TableExtensionStartup : WorkerExtensionStartup
    {
        /// <summary>
        /// Configure table extension startup.
        /// </summary>
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory
            applicationBuilder.Services.AddOptions<TablesBindingOptions>();
            applicationBuilder.Services.AddSingleton<IConfigureOptions<TablesBindingOptions>, TablesBindingOptionsSetup>();
        }
    }
}
