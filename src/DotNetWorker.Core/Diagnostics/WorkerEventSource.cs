// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Functions.Worker.Core.Diagnostics
{
    [EventSource(Name = "Microsoft-AzureFunctions-Worker", Guid = "DCCCCC7B-F393-4852-96AE-BB6769A266C4")]
    internal sealed class WorkerEventSource : EventSource
    {
        [Event(1001)]
        public void StartupHookInit()
        {
            WriteEvent(1001);
        }

        [Event(1002)]
        public void StartupHookPreJitStart(string jitFilePath)
        {
            WriteEvent(1002, jitFilePath);
        }

        [Event(1003)]
        public void StartupHookPreJitStop(int successfulPrepares, int failedPrepares)
        {
            WriteEvent(1003, successfulPrepares, failedPrepares);
        }

        [Event(1004)]
        public void StartupHookWaitForSpecializationRequestStart()
        {
            WriteEvent(1004);
        }

        [Event(1005)]
        public void StartupHookReceivedContinueExecutionSignalFromFunctionsNetHost()
        {
            WriteEvent(1005);
        }

        internal static readonly WorkerEventSource Log = new WorkerEventSource();
    }
}
