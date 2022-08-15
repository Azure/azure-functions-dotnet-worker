// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    /// <summary>
    /// Represents an interface for sending logs directly to the Functions host.
    /// </summary>
    internal interface IWorkerDiagnostics
    {
        void OnApplicationCreated(WorkerInformation workerInfo);

        void OnFunctionLoaded(FunctionDefinition definition);
    }
}
