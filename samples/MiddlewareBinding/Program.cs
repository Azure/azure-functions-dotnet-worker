// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;
using MiddlewareBinding;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerApplication =>
    {
        // Only run the middleware for functions that have a Service Bus trigger.
        workerApplication.UseWhen<ServiceBusMessageMiddleware>(context =>
            context.FunctionDefinition.InputBindings.Values.Any(b => b.Type == "serviceBusTrigger"));
    })
    .Build();

host.Run();
