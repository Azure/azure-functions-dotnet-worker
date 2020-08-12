using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.DotNetWorker;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorkerTests
{
    internal class TestFunctionExecutionContext : FunctionExecutionContext, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public override RpcTraceContext TraceContext { get; set; }

        public override InvocationRequest InvocationRequest { get; set; }

        public override IServiceProvider InstanceServices { get; set; }

        public override FunctionDefinition FunctionDefinition { get; set; }

        public override object InvocationResult { get; set; }

        public override InvocationLogger Logger { get; set; }

        public override List<ParameterBinding> ParameterBindings { get; set; } = new List<ParameterBinding>();

        public override IDictionary<object, object> Items { get; set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
