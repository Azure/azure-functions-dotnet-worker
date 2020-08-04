using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class FunctionExecutionContext : IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IServiceScope _instanceServicesScope;
        private IServiceProvider _instanceServices;

        public FunctionExecutionContext(IServiceScopeFactory serviceScopeFactory, InvocationRequest invocationRequest)
        {
            _serviceScopeFactory = serviceScopeFactory;
            InvocationRequest = invocationRequest;
            TraceContext = invocationRequest.TraceContext;
        }

        // created on construction
        public RpcTraceContext TraceContext { get; }
        public InvocationRequest InvocationRequest { get; }
        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        // settable properties
        public FunctionDescriptor FunctionDescriptor { get; set; }
        public object InvocationResult { get; set; }
        public InvocationLogger Logger { get; set; }
        public List<ParameterBinding> ParameterBindings { get; set; } = new List<ParameterBinding>();

        public IServiceProvider InstanceServices
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
