// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

/// <summary>
/// This code is called by the .NET infrastructure the following MUST remain unchanged:
///  - The type name must be StartupHook
///  - The type must be at the top level, with no namespaces
///  - The type must be internal
///  - The initialization method must be named Initialize
///  - The initialization method must be static, with a void return.
///  For more information, see: https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-startup-hook.md
/// </summary>
internal class StartupHook
{
    public static void Initialize()
    {
        // Time to wait between checks, in ms.
        const int SleepTime = 500;
        const int MaxWaitCycles = (60 * 1000) / SleepTime;

        Console.WriteLine($"Azure Functions .NET Worker (PID: { Environment.ProcessId }) initialized in debug mode. Waiting for debugger to attach...");

        string? jsonOutputEnabled = Environment.GetEnvironmentVariable("FUNCTIONS_ENABLE_JSON_OUTPUT");

        if (jsonOutputEnabled is not null && string.Equals(jsonOutputEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{{ \"workerProcessId\" : { Environment.ProcessId } }}");
        }

        static bool WaitOnDebugger(int cycle)
        {
            if (Debugger.IsAttached)
            {
                return false;
            }

            if (cycle > MaxWaitCycles)
            {
                Console.WriteLine("A debugger was not attached within the expected time limit. The process will continue without a debugger.");

                return false;
            }

            return true;
        }

        for (int i = 0; WaitOnDebugger(i); i++)
        {
            Thread.Sleep(SleepTime);
        }
    }
}
