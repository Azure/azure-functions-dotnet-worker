// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AspNetCoreFunctionApp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

new HostBuilder()
    .ConfigureFunctionsWebApplication(worker => worker.UseMiddleware<MarkerMiddleware>())
    .ConfigureServices(services => services.AddSingleton(new AppMarker("fixture-service")))
    .Build()
    .Run();

namespace AspNetCoreFunctionApp
{
    public partial class Program
    {
    }

    public sealed record AppMarker(string Value);
}
