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

        string? debuggerWaitEnabled = Environment.GetEnvironmentVariable("FUNCTIONS_ENABLE_DEBUGGER_WAIT");
        string? jsonOutputEnabled = Environment.GetEnvironmentVariable("FUNCTIONS_ENABLE_JSON_OUTPUT");

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

        if (string.Equals(jsonOutputEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"azfuncjsonlog:{{ \"name\":\"dotnet-worker-startup\", \"workerProcessId\" : { Environment.ProcessId } }}");
        }

        if (string.Equals(debuggerWaitEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Azure Functions .NET Worker (PID: { Environment.ProcessId }) initialized in debug mode. Waiting for debugger to attach...");

            for (int i = 0; WaitOnDebugger(i); i++)
            {
                Thread.Sleep(SleepTime);
            }
        }
    }
}
