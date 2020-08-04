using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.DotNetWorker
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

        public Task<InvocationResponse> InvokeFunctionAsync(InvocationRequest request)
        {
            return _functionBroker.InvokeAsync(request);
        }

        public Task<FunctionLoadResponse> LoadFunctionAsync(FunctionLoadRequest request)
        {
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
