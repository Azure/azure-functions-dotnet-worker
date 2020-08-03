using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class FunctionBroker : IFunctionBroker
    {
        private readonly ParameterConverterManager _converterManager;
        private FunctionsHostOutputChannel _workerChannel;
        private Dictionary<string, FunctionDescriptor> _functionMap = new Dictionary<string, FunctionDescriptor>();
        private IFunctionInstanceFactory _functionInstanceFactory;
        private IFunctionInvoker _functionInvoker;

        public FunctionBroker(ParameterConverterManager converterManager, IFunctionInvoker functionInvoker, FunctionsHostChannelWriter workerChannel, IFunctionInstanceFactory functionInstanceFactory)
        {
            _converterManager = converterManager;
            _workerChannel = workerChannel;
            _functionInstanceFactory = functionInstanceFactory;
            _functionInvoker = functionInvoker;
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDescriptor functionDescriptor = new FunctionDescriptor(functionLoadRequest);

            _functionMap.Add(functionDescriptor.FunctionID, functionDescriptor);
        }

        public object InvokeAsync(InvocationRequest invocationRequest, out List<ParameterBinding> parameterBindings)
        {
            parameterBindings = new List<ParameterBinding>();
            FunctionDescriptor functionDescriptor = _functionMap[invocationRequest.FunctionId];
            FunctionExecutionContext executionContext = new FunctionExecutionContext(functionDescriptor, _converterManager, invocationRequest, _workerChannel.Writer, _functionInstanceFactory);

            var result = _functionInvoker.InvokeAsync(executionContext);
            
            if(executionContext.ParameterBindings != null) 
            {
                parameterBindings = executionContext.ParameterBindings;
            }

            return result.Result;
        }
    }

}
