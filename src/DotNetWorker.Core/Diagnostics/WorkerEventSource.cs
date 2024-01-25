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
            //if (IsEnabled())
            {
                WriteEvent(1001);
            }
        }

        internal static readonly WorkerEventSource Log = new WorkerEventSource();
    }
}
