// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker;

public class ServiceBusExtensionStartup : WorkerExtensionStartup
{
    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    {
        if (applicationBuilder == null)
        {
            throw new ArgumentNullException(nameof(applicationBuilder));
        }

        applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory

        applicationBuilder.Services.Configure<WorkerOptions>((workerOption) =>
        {
            workerOption.InputConverters.RegisterAt<ServiceBusReceivedMessageConverter>(0);
        });
    }
}