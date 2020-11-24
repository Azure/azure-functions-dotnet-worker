using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    public abstract class FunctionExecutionContext
    {
        public abstract RpcTraceContext TraceContext { get; set; }

        public abstract InvocationRequest InvocationRequest { get; set; }

        public abstract IServiceProvider InstanceServices { get; set; }

        public abstract FunctionDefinition FunctionDefinition { get; set; }

        public abstract object InvocationResult { get; set; }

        public abstract InvocationLogger Logger { get; set; }

        public abstract List<ParameterBinding> ParameterBindings { get; set; }

        public abstract IDictionary<object, object> Items { get; set; }
    }
}
