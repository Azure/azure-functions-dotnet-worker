using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    public abstract class FunctionExecutionContext
    {
        // created on construction
        public abstract RpcTraceContext TraceContext { get; }
        public abstract InvocationRequest InvocationRequest { get; }
        public abstract IServiceProvider InstanceServices { get; }

        // settable properties
        public abstract FunctionDescriptor FunctionDescriptor { get; set; }
        public abstract object InvocationResult { get; set; }
        public abstract InvocationLogger Logger { get; set; }
        public abstract List<ParameterBinding> ParameterBindings { get; set; }
        public abstract IDictionary<object, object> Items { get; set; }
    }
}
