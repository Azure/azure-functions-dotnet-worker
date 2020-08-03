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

        public async Task<InvocationResponse> InvokeFunctionAsync(InvocationRequest request)
        {
            InvocationResponse response = new InvocationResponse
            {
                InvocationId = request.InvocationId
            };

            try
            {
                FunctionExecutionContext executionContext = await _functionBroker.InvokeAsync(request);
                var parameterBindings = executionContext.ParameterBindings;
                var result = executionContext.InvocationResult;

                foreach (var paramBinding in parameterBindings)
                {
                    response.OutputData.Add(paramBinding);
                }
                if (result != null)
                {
                    var returnVal = result.ToRpc();

                    response.ReturnValue = returnVal;
                }

                response.Result = new StatusResult { Status = Status.Success };
            }
            catch (Exception)
            {
                response.Result = new StatusResult { Status = Status.Failure };
            }

            return response;
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
