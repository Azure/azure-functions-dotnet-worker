// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class Constants
    {
        public const string AzureSignalRConnectionStringName = "AzureSignalRConnectionString";
        public const string ServiceTransportTypeName = "AzureSignalRServiceTransportType";
        public const string AzureSignalREndpoints = "Azure:SignalR:Endpoints";

        public const string FunctionsWorkerProductInfoKey = "func";
        public const string DotnetIsolatedWorker = "dotnet-isolated";
    }
}
