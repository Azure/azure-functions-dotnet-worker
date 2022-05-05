// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
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
                    // Register our custom middlewares with the worker

                    workerApplication.UseMiddleware<ExceptionHandlingMiddleware>();

                    workerApplication.UseMiddleware<MyCustomMiddleware>();

                    workerApplication.UseWhen<StampHttpHeaderMiddleware>((context) =>
                    {
                        // We want to use this middleware only for http trigger invocations.
                        return context.FunctionDefinition.InputBindings.Values
                                      .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
                    });
                })
                .Build();
            //</docsnippet_middleware_register>
            host.Run();
        }
    }
}
