using System.Collections.Generic;
using System.Threading.Channels;
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

        public FunctionExecutionContext(InvocationRequest invocationRequest, string funcName, ChannelWriter<StreamingMessage> channelWriter)
        {
            InvocationId = invocationRequest.InvocationId;
            FunctionName = funcName;
            TraceContext = invocationRequest.TraceContext;
            Logger = new InvocationLogger(InvocationId, channelWriter);
        }

        public FunctionExecutionContext(string invocationId, string funcName, RpcTraceContext traceContext)
        {
            FunctionName = funcName;
            InvocationId = invocationId;
            TraceContext = traceContext;
        }
        public string FunctionName { get; private set; }
        public string InvocationId { get; private set; }
        public InvocationLogger Logger { get; private set; }
        public RpcTraceContext TraceContext { get; private set; }

        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this invocation.
        /// </summary>
        public IDictionary<object, object> Items { get; }
    }
}
