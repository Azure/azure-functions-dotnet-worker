// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    public class Constants
    {
        public const string DefaultConnectionStringName = "AzureSignalRConnectionString";
        internal const string ServiceTransportTypeName = "AzureSignalRServiceTransportType";
        internal const string AzureSignalREndpoints = "Azure:SignalR:Endpoints";

        internal const string FunctionsWorkerProductInfoKey = "func";
        internal const string DotnetIsolatedWorker = "dotnet-isolated";
    }
}
