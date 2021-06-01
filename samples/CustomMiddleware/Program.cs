// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CustomMiddleware
{
    public class Program
    {
        public static void Main()
        {
            //<docsnippet_middleware_register>
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(workerApplication =>
                {
                    // Register our custom middleware with the worker
                    workerApplication.UseMiddleware<MyCustomMiddleware>();
                })
                .Build();
            //</docsnippet_middleware_register>
            host.Run();
        }
    }
}
