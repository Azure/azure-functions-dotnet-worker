using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Functions.Worker.Core.Diagnostics
{
    [EventSource(Name = "Microsoft-AzureFunctions-Worker", Guid = "BD0E0962-2562-4751-8191-DAFE3750FD38")]
    internal sealed class WorkerEventSource : EventSource
    {
        [Event(1001)]
        public void StartupHookInit() 
        {
            Console.WriteLine("Inside StartupHookInit");
            if (IsEnabled())
            {
                Console.WriteLine("Inside StartupHookInit - calling WriteEvent");

                WriteEvent(1001);
            }
        }

        internal static readonly WorkerEventSource Log = new WorkerEventSource();
    }
}
