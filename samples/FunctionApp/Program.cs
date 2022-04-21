// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public sealed class MyHttpMiddleware2 : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            ILogger logger = context.GetLogger<MyHttpMiddleware2>();

            logger.LogInformation($"From MyHttpMiddleware2 {DateTime.Now}");

            await next(context);

            // Stamp a response header.
            context.GetHttpResponseData().Headers.Add("X-REQ-ID", Guid.NewGuid().ToString());
        }
    }
    public sealed class MyHttpMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            ILogger logger = context.GetLogger<MyHttpMiddleware>();

            logger.LogInformation($"From MyHttpMiddleware {DateTime.Now}");

            await next(context);

            // Stamp a response header.
            context.GetHttpResponseData().Headers.Add("X-REQ-ID", Guid.NewGuid().ToString());
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // #if DEBUG
            //          Debugger.Launch();
            // #endif
            //<docsnippet_startup>
            var host = new HostBuilder()
                //<docsnippet_configure_defaults>
                .ConfigureFunctionsWorkerDefaults((builder) =>
                {
                    builder.UseMiddleware<MyHttpMiddleware>();

                    //builder.Use((context) =>
                    //{
                    //    return next.Invoke();
                    //});

                    builder.UseWhen((context) =>
                    {
                        //var re = await context.GetHttpRequestDataAsync();

                        return true;

                    }, (context, next) =>
                     {
                         return next.Invoke();
                     });

                })
                //</docsnippet_configure_defaults>
                //<docsnippet_dependency_injection>
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                })
                //</docsnippet_dependency_injection>
                .Build();
            //</docsnippet_startup>

            //<docsnippet_host_run>
            await host.RunAsync();
            //</docsnippet_host_run>
        }
    }
}
