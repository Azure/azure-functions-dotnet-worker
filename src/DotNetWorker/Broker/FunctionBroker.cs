using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.Worker
{
    internal class FunctionBroker : IFunctionBroker
    {
        private ConcurrentDictionary<string, FunctionDefinition> _functionMap = new ConcurrentDictionary<string, FunctionDefinition>();
        private FunctionExecutionDelegate _functionExecutionDelegate;
        private IFunctionExecutionContextFactory _functionExecutionContextFactory;
        private IFunctionDefinitionFactory _functionDescriptorFactory;

        public FunctionBroker(FunctionExecutionDelegate functionExecutionDelegate, IFunctionExecutionContextFactory functionExecutionContextFactory, IFunctionDefinitionFactory functionDescriptorFactory)
        {
            _functionExecutionDelegate = functionExecutionDelegate;
            _functionExecutionContextFactory = functionExecutionContextFactory;
            _functionDescriptorFactory = functionDescriptorFactory;
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDefinition functionDefinition = _functionDescriptorFactory.Create(functionLoadRequest);
            _functionMap.TryAdd(functionDefinition.Metadata.FunctionId, functionDefinition);
        }

        public async Task<InvocationResponse> InvokeAsync(InvocationRequest invocationRequest)
        {
            InvocationResponse response = new InvocationResponse
            {
                InvocationId = invocationRequest.InvocationId
            };

            FunctionExecutionContext executionContext = null;

            try
            {
                executionContext = _functionExecutionContextFactory.Create(invocationRequest);
                executionContext.FunctionDefinition = _functionMap[invocationRequest.FunctionId];

                await _functionExecutionDelegate(executionContext);
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
            finally
            {
                (executionContext as IDisposable)?.Dispose();
            }

            return response;
        }
    }

}
