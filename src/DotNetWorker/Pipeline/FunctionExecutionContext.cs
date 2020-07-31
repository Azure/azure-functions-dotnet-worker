using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Channels;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class FunctionExecutionContext
    {
        public FunctionExecutionContext()
        {
            Items = new Dictionary<object, object>();
        }

        public FunctionExecutionContext(FunctionDescriptor functionDescriptor, ParameterConverterManager paramConverterManager, InvocationRequest invocationRequest, ChannelWriter<StreamingMessage> channelWriter, IFunctionInstanceFactory functionInstanceFactory)
        {
            TraceContext = invocationRequest.TraceContext;
            Logger = new InvocationLogger(invocationRequest.InvocationId, channelWriter);
            FunctionDescriptor = functionDescriptor;
            FunctionInstanceFactory = functionInstanceFactory;
            InvocationRequest = invocationRequest;
            ParameterConverterManager = paramConverterManager;
        }

        public FunctionDescriptor FunctionDescriptor { get; private set; }
        public RpcTraceContext TraceContext { get; private set; }
        public InvocationLogger Logger { get; private set; }
        public List<ParameterBinding> ParameterBindings { get; set; }
        public InvocationRequest InvocationRequest { get; private set; }
        public ChannelWriter<StreamingMessage> ChannelWriter {get; private set;} 
        public IFunctionInstanceFactory FunctionInstanceFactory { get; private set; }
        public ParameterConverterManager ParameterConverterManager { get; private set; }
        public object InvocationResult { get; set; }

        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this invocation.
        /// </summary>
        public IDictionary<object, object> Items { get; }
    }
}
