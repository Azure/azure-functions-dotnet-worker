using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.FunctionDescriptor;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class FunctionBroker : IFunctionBroker
    {
        private Dictionary<string, IFunctionDescriptor> _functionMap = new Dictionary<string, IFunctionDescriptor>();
        private FunctionExecutionDelegate _functionExecutionDelegate;
        private IFunctionExecutionContextFactory _functionExecutionContextFactory;
        private IFunctionDescriptorFactory _functionDescriptorFactory;

        public FunctionBroker(FunctionExecutionDelegate functionExecutionDelegate, IFunctionExecutionContextFactory functionExecutionContextFactory, IFunctionDescriptorFactory functionDescriptorFactory)
        {
            _functionExecutionDelegate = functionExecutionDelegate;
            _functionExecutionContextFactory = functionExecutionContextFactory;
            _functionDescriptorFactory = functionDescriptorFactory;
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            IFunctionDescriptor functionDescriptor = _functionDescriptorFactory.Create(functionLoadRequest);
            _functionMap.Add(functionDescriptor.FunctionID, functionDescriptor);
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
                executionContext.FunctionDescriptor = _functionMap[invocationRequest.FunctionId];

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
