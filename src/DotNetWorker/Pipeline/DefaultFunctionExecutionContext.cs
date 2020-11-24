using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    public class DefaultFunctionExecutionContext : FunctionExecutionContext, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private IServiceScope? _instanceServicesScope;
        private IServiceProvider? _instanceServices;

        public DefaultFunctionExecutionContext(IServiceScopeFactory serviceScopeFactory, InvocationRequest invocationRequest)
        {
            _serviceScopeFactory = serviceScopeFactory;
            InvocationRequest = invocationRequest;
            TraceContext = invocationRequest.TraceContext;
        }

        public override RpcTraceContext TraceContext { get; set; }

        public override InvocationRequest InvocationRequest { get; set; }

        public override FunctionDefinition FunctionDefinition { get; set; }

        public override object InvocationResult { get; set; }

        public override InvocationLogger Logger { get; set; }

        public override List<ParameterBinding> ParameterBindings { get; set; } = new List<ParameterBinding>();

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IServiceProvider? InstanceServices
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

            set { _instanceServices = value; }
        }

        public void Dispose()
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
