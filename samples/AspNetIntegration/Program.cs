// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license informations

using AspNetIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

FunctionsApplicationBuilder funcBuilder = FunctionsApplication.CreateBuilder(args);
funcBuilder.ConfigureFunctionsWebApplication();

funcBuilder.UseWhen<RoutingMiddleware>((context) =>
{
    // We want to use this middleware only for http trigger invocations.
    return context.FunctionDefinition.InputBindings.Values
                    .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
});

IHost app = funcBuilder.Build();
app.Run();
