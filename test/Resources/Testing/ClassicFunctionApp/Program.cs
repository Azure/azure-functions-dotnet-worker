// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassicFunctionApp;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((context, services) =>
                services.AddSingleton(new ResolvedFactorySetting(context.Configuration["FactorySetting"])))
            .Build();

        host.Run();
    }
}

public sealed record ResolvedFactorySetting(string? Value);
