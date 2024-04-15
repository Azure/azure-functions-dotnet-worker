// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

namespace FunctionsNetHost.Diagnostics
{
    // Use 8000-8999 for events from FunctionsNetHost.
    [EventSource(Name = Constants.EventSourceName, Guid = Constants.EventSourceGuid)]
    public sealed class AppLoaderEventSource : EventSource
    {
        [Event(8001)]
        public void HostFxrLoadStart(string hostFxrPath)
        {
            if (IsEnabled())
            {
                WriteEvent(8001, hostFxrPath);
            }
        }

        [Event(8002)]
        public void HostFxrLoadStop()
        {
            if (IsEnabled())
            {
                WriteEvent(8002);
            }
        }

        [Event(8003)]
        public void HostFxrInitializeForDotnetCommandLineStart(string assemblyPath)
        {
            if (IsEnabled())
            {
                WriteEvent(8003, assemblyPath);
            }
        }

        [Event(8004)]
        public void HostFxrInitializeForDotnetCommandLineStop()
        {
            if (IsEnabled())
            {
                WriteEvent(8004);
            }
        }

        [Event(8005)]
        public void HostFxrRunAppStart()
        {
            if (IsEnabled())
            {
                WriteEvent(8005);
            }
        }

        [Event(8006)]
        public void AssembliesPreloaded(int assemblyCount, int successfulPreloadCount, int failedPreloadCount)
        {
            if (IsEnabled())
            {
                WriteEvent(8006, assemblyCount, successfulPreloadCount, failedPreloadCount);
            }
        }

        [Event(8007)]
        public void ApplicationMainStartedSignalReceived()
        {
            if (IsEnabled())
            {
                WriteEvent(8007);
            }
        }

        [Event(8008)]
        public void SpecializationRequestReceived()
        {
            if (IsEnabled())
            {
                WriteEvent(8008);
            }
        }

        [Event(8009)]
        public void ColdStartRequestFunctionInvocationStart()
        {
            if (IsEnabled())
            {
                WriteEvent(8009);
            }
        }

        [Event(8010)]
        public void ColdStartRequestFunctionInvocationStop()
        {
            if (IsEnabled())
            {
                WriteEvent(8010);
            }
        }

        [Event(8011)]
        public void FunctionMetadataReqStart()
        {
            if (IsEnabled())
            {
                WriteEvent(8011);
            }
        }
        [Event(8012)]
        public void FunctionMetadataReqStop()
        {
            if (IsEnabled())
            {
                WriteEvent(8012);
            }
        }

        [Event(8013)]
        public void FunctionLoadReqStart(string functionId)
        {
            if (IsEnabled())
            {
                WriteEvent(8013, functionId);
            }
        }

        [Event(8014)]
        public void FunctionLoadReqStop(string functionId)
        {
            if (IsEnabled())
            {
                WriteEvent(8014, functionId);
            }
        }
        [Event(8015)]
        public void NetHostWorkerInitCompleted()
        {
            if (IsEnabled())
            {
                WriteEvent(8015);
            }
        }
        public static readonly AppLoaderEventSource Log = new();
    }
}
