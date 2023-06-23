// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license informations

using AspNetIntegration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        // can still register middleware and use this extension method the same way
        // .ConfigureFunctionsWorkerDefaults() is used
        builder.UseMiddleware<FooMiddleware>();
    })
    .Build();

host.Run();
