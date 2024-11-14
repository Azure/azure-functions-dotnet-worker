// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
    const string StartupHooksEnvVar = "DOTNET_STARTUP_HOOKS";
    private static readonly string _startupSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
    private static readonly string? _assemblyName = typeof(StartupHook).Assembly.GetName().Name;

    public static void Initialize()
    {
        // Time to wait between checks, in ms.
        const int SleepTime = 500;
        const int MaxWaitCycles = (60 * 1000) / SleepTime;

        RemoveSelfFromStartupHooks();
        string? debuggerWaitEnabled = Environment.GetEnvironmentVariable("FUNCTIONS_ENABLE_DEBUGGER_WAIT");
        string? jsonOutputEnabled = Environment.GetEnvironmentVariable("FUNCTIONS_ENABLE_JSON_OUTPUT");
#if NET6_0_OR_GREATER
        int processId = Environment.ProcessId;
#else
        int processId = Process.GetCurrentProcess().Id;
#endif

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
            Console.WriteLine($"azfuncjsonlog:{{ \"name\":\"dotnet-worker-startup\", \"workerProcessId\" : {processId} }}");
        }

        if (string.Equals(debuggerWaitEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Azure Functions .NET Worker (PID: {processId}) initialized in debug mode. Waiting for debugger to attach...");

            for (int i = 0; WaitOnDebugger(i); i++)
            {
                Thread.Sleep(SleepTime);
            }
        }
    }

    internal static void RemoveSelfFromStartupHooks()
    {
        string? startupHooks = Environment.GetEnvironmentVariable(StartupHooksEnvVar);
        if (string.IsNullOrEmpty(startupHooks))
        {
            // If this call happened, we are clearly part of this environment variable.
            // This is mostly to make strict-nulls happy.
            return;
        }

        // netstandard2.0 has no StringSplitOptions overload.
        IEnumerable<string> parts = startupHooks.Split(_startupSeparator[0])
            .Where(x => !string.Equals(x, _assemblyName, StringComparison.Ordinal));
        string newValue = string.Join(_startupSeparator, parts);
        Environment.SetEnvironmentVariable(StartupHooksEnvVar, newValue);
    }
}
