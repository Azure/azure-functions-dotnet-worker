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
        private readonly ParameterConverterManager _converterManager;
        private ChannelWriter<StreamingMessage> _writerChannel;
        private Dictionary<string, FunctionDescriptor> _functionMap = new Dictionary<string, FunctionDescriptor>();
        private IFunctionInstanceFactory _functionInstanceFactory;
        private IFunctionInvoker _functionInvoker;
        private FunctionExecutionDelegate _functionExecutionDelegate;

        public FunctionBroker(ParameterConverterManager converterManager, IFunctionInvoker functionInvoker, FunctionsHostOutputChannel outputChannel, IFunctionInstanceFactory functionInstanceFactory, FunctionExecutionDelegate functionExecutionDelegate)
        {
            _converterManager = converterManager;
            _functionInstanceFactory = functionInstanceFactory;
            _functionInvoker = functionInvoker;
            _writerChannel = outputChannel.Channel.Writer;
            _functionExecutionDelegate = functionExecutionDelegate;
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDescriptor functionDescriptor = new FunctionDescriptor(functionLoadRequest);

            _functionMap.Add(functionDescriptor.FunctionID, functionDescriptor);
        }

        public Task<InvocationResponse> InvokeAsync(InvocationRequest invocationRequest)
        {
            InvocationResponse response = new InvocationResponse
            {
                InvocationId = invocationRequest.InvocationId
            };

            try
            {
                FunctionDescriptor functionDescriptor = _functionMap[invocationRequest.FunctionId];
                FunctionExecutionContext executionContext = new FunctionExecutionContext(functionDescriptor, _converterManager, invocationRequest, _writerChannel, _functionInstanceFactory);

                _functionExecutionDelegate(executionContext);
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

            return Task.FromResult(response);
        }
    }

}
