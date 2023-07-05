// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license informations

using AspNetIntegration;
using Microsoft.Extensions.Hosting;

//<docsnippet_aspnet_registration>
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        // can still register middleware and use this extension method the same way
        // .ConfigureFunctionsWorkerDefaults() is used
        builder.UseWhen<FooMiddleware>((context)=>
        {
            // We want to use this middleware only for http trigger invocations.
            return context.FunctionDefinition.InputBindings.Values
                          .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
        });
    })
    .Build();

host.Run();
//</docsnippet_aspnet_registration>
