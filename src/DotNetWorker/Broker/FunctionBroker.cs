using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
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

        public async Task<InvocationResponse> InvokeAsync(FunctionInvocation invocation)
        {
            // TODO: File InvocationResponse removal issue
            InvocationResponse response = new InvocationResponse
            {
                InvocationId = invocation.InvocationId
            };

            FunctionExecutionContext? executionContext = null;

            try
            {
                executionContext = _functionExecutionContextFactory.Create(invocation, _functionMap[invocation.FunctionId]);

                await _functionExecutionDelegate(executionContext);

                var parameterBindings = executionContext.OutputBindings;
                var result = executionContext.InvocationResult;

                foreach (var paramBinding in parameterBindings)
                {
                    // TODO: ParameterBinding shouldn't happen here

                    foreach (var binding in executionContext.OutputBindings)
                    {
                        dynamic d = binding.Value;
                        var rpcVal = d.GetValue();
                        var parameterBinding = new ParameterBinding
                        {
                            Name = binding.Key,
                            Data = RpcExtensions.ToRpc(rpcVal)
                        };
                        response.OutputData.Add(parameterBinding);
                    }
                }
                if (result != null)
                {
                    var returnVal = result.ToRpc();

                    response.ReturnValue = returnVal;
                }

                response.Result = new StatusResult { Status = Status.Success };
            }
            catch (Exception ex)
            {
                response.Result = new StatusResult
                {
                    Exception = ex.ToRpcException(),
                    Status = Status.Failure
                };
            }
            finally
            {
                (executionContext as IDisposable)?.Dispose();
            }

            return response;
        }
    }

}
