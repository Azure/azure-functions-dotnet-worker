// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IHostRequestHandler
    {
        Task<WorkerInitResponse> InitializeWorkerAsync(WorkerInitRequest request);

        Task<FunctionLoadResponse> LoadFunctionAsync(FunctionLoadRequest request);

        Task<InvocationResponse> InvokeFunctionAsync(FunctionInvocation invocation);

        Task<FunctionEnvironmentReloadResponse> ReloadEnvironmentAsync(FunctionEnvironmentReloadRequest request);
    }
}
