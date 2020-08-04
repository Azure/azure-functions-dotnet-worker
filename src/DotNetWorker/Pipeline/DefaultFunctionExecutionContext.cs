using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class DefaultFunctionExecutionContext : FunctionExecutionContext
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IServiceScope _instanceServicesScope;
        private IServiceProvider _instanceServices;

        public DefaultFunctionExecutionContext(IServiceScopeFactory serviceScopeFactory, InvocationRequest invocationRequest)
        {
            _serviceScopeFactory = serviceScopeFactory;
            InvocationRequest = invocationRequest;
            TraceContext = invocationRequest.TraceContext;
        }

        // created on construction
        public override RpcTraceContext TraceContext { get; }
        public override InvocationRequest InvocationRequest { get; }

        // settable properties
        public override FunctionDescriptor FunctionDescriptor { get; set; }
        public override object InvocationResult { get; set; }
        public override InvocationLogger Logger { get; set; }
        public override List<ParameterBinding> ParameterBindings { get; set; } = new List<ParameterBinding>();
        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IServiceProvider InstanceServices
        {
            get
            {
                if (_instanceServicesScope == null && _serviceScopeFactory != null)
                {
                    _instanceServicesScope = _serviceScopeFactory.CreateScope();
                    _instanceServices = _instanceServicesScope.ServiceProvider;
                }

                return _instanceServices;
            }
        }

        public override void Dispose()
        {
            if (_instanceServicesScope != null)
            {
                _instanceServicesScope.Dispose();
            }

            _instanceServicesScope = null;
            _instanceServices = null;
        }
    }
}
