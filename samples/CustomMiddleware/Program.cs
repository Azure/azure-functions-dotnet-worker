// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CustomMiddleware;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

//<docsnippet_middleware_register>
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register our custom middlewares with the worker

builder.UseMiddleware<ExceptionHandlingMiddleware>();
builder.UseWhen<StampHttpHeaderMiddleware>((context) =>
{
    // We want to use this middleware only for http trigger invocations.
    return context.FunctionDefinition.InputBindings.Values
                    .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
});

var host = builder.Build();
//</docsnippet_middleware_register>
host.Run();

