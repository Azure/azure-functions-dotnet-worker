// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

namespace FunctionsNetHost.Diagnostics
{
    // Use 8000-8999 for events from FunctionsNetHost.

    [EventSource(Name = Constants.AzureFunctionsEventSourceName, Guid = Constants.AzureFunctionsEventSourceGuid)]
    public sealed class AppLoaderEventSource : EventSource
    {
        [Event(8001)]
        public void HostFxrLoadStart(string HostFxrPath)
        {
            if (IsEnabled())
            {
                WriteEvent(8001, HostFxrPath);
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
        public void HostFxrInitializeForDotnetCommandLineStart(string AssemblyPath)
        {
            if (IsEnabled())
            {
                WriteEvent(8003, AssemblyPath);
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

        public static readonly AppLoaderEventSource Log = new();
    }
}
