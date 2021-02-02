// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultHostRequestHandler : IHostRequestHandler
    {
        private readonly IFunctionBroker _functionBroker;

        public DefaultHostRequestHandler(IFunctionBroker functionBroker)
        {
            _functionBroker = functionBroker ?? throw new ArgumentNullException(nameof(functionBroker));
        }

        public Task<WorkerInitResponse> InitializeWorkerAsync(WorkerInitRequest request)
        {
            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = Status.Success }
            };

            return Task.FromResult(response);
        }

        public Task<InvocationResponse> InvokeFunctionAsync(FunctionInvocation invocation)
        {
            return _functionBroker.InvokeAsync(invocation);
        }

        public Task<FunctionLoadResponse> LoadFunctionAsync(FunctionLoadRequest request)
        {
            // instead, use request.Metadata.IsProxy
            if (!string.IsNullOrEmpty(request.Metadata?.ScriptFile))
            {
                _functionBroker.AddFunction(request);
            }

            var response = new FunctionLoadResponse
            {
                FunctionId = request.FunctionId,
                Result = new StatusResult { Status = Status.Success }
            };

            return Task.FromResult(response);
        }

        public Task<FunctionEnvironmentReloadResponse> ReloadEnvironmentAsync(FunctionEnvironmentReloadRequest request)
        {
            var response = new FunctionEnvironmentReloadResponse
            {
                Result = new StatusResult { Status = Status.Success }
            };

            return Task.FromResult(response);
        }
    }
}
