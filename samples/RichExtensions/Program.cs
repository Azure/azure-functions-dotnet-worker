// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleApp
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine($"Azure Functions .NET Worker (PID: { Environment.ProcessId }) initialized in debug mode. Waiting for debugger to attach...");

            while (!Debugger.IsAttached){
                Thread.Sleep(500);
            }

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
        }
    }
}
