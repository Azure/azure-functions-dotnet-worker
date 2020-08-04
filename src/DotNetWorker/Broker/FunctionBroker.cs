using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class FunctionBroker : IFunctionBroker
    {
        private Dictionary<string, FunctionDescriptor> _functionMap = new Dictionary<string, FunctionDescriptor>();
        private FunctionExecutionDelegate _functionExecutionDelegate;
        private IFunctionExecutionContextFactory _functionExecutionContextFactory;

        public FunctionBroker(FunctionExecutionDelegate functionExecutionDelegate, IFunctionExecutionContextFactory functionExecutionContextFactory)
        {
            _functionExecutionDelegate = functionExecutionDelegate;
            _functionExecutionContextFactory = functionExecutionContextFactory;
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDescriptor functionDescriptor = new FunctionDescriptor(functionLoadRequest);

            _functionMap.Add(functionDescriptor.FunctionID, functionDescriptor);
        }

        public async Task<InvocationResponse> InvokeAsync(InvocationRequest invocationRequest)
        {
            InvocationResponse response = new InvocationResponse
            {
                InvocationId = invocationRequest.InvocationId
            };

            FunctionExecutionContext executionContext = _functionExecutionContextFactory.Create(invocationRequest);
            executionContext.FunctionDescriptor = _functionMap[invocationRequest.FunctionId];

            try
            {
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
                executionContext = (DefaultFunctionExecutionContext) executionContext; 
                executionContext.Dispose();
            }
      
            return response;
        }
    }

}
