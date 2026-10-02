// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
string? imperativeSetting = builder.Configuration["FactorySetting"];
builder.Services.AddSingleton(new ModernFunctionApp.ImperativeSettingSnapshot(imperativeSetting));
builder.Services.AddSingleton<ModernFunctionApp.ResolvedFactorySetting>(services =>
    new ModernFunctionApp.ResolvedFactorySetting(services.GetRequiredService<IConfiguration>()["FactorySetting"]));
builder.Build().Run();

namespace ModernFunctionApp
{
    public partial class Program
    {
    }

    public sealed record ImperativeSettingSnapshot(string? Value);

    public sealed record ResolvedFactorySetting(string? Value);
}
