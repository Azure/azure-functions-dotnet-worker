// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionsApplication
    {
        Task<WorkerInitResponse> InitializeWorkerAsync(WorkerInitRequest request);

        void LoadFunction(FunctionDefinition definition);

        Task<InvocationResponse> InvokeFunctionAsync(FunctionContext context);

        Task<FunctionEnvironmentReloadResponse> ReloadEnvironmentAsync(FunctionEnvironmentReloadRequest request);

        FunctionContext CreateContext(IInvocationFeatures features);
    }
}
