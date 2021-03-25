// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    internal sealed class DefaultFunctionContext : FunctionContext, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly FunctionInvocation _invocation;

        private IServiceScope? _instanceServicesScope;
        private IServiceProvider? _instanceServices;
        private BindingContext? _bindingContext;

        public DefaultFunctionContext(IServiceScopeFactory serviceScopeFactory, IInvocationFeatures features)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            Features = features ?? throw new ArgumentNullException(nameof(features));

            _invocation = features.Get<FunctionInvocation>() ?? throw new InvalidOperationException($"The '{nameof(FunctionInvocation)}' feature is required.");
            FunctionDefinition = features.Get<FunctionDefinition>() ?? throw new InvalidOperationException($"The {nameof(Worker.FunctionDefinition)} feature is required.");
        }


        public override string InvocationId => _invocation.Id;

        public override string FunctionId => _invocation.FunctionId;

        public override FunctionDefinition FunctionDefinition { get; }

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IInvocationFeatures Features { get; }

        public override IServiceProvider InstanceServices
        {
            get
            {
                if (_instanceServicesScope == null)
                {
                    _instanceServicesScope = _serviceScopeFactory.CreateScope();
                    _instanceServices = _instanceServicesScope.ServiceProvider;
                }

                return _instanceServices!;
            }

            set => _instanceServices = value;
        }

        public override TraceContext TraceContext => _invocation.TraceContext;

        public override BindingContext BindingContext => _bindingContext ??= new DefaultBindingContext(this);

        public override RetryContext RetryContext => Features.GetRequired<IExecutionRetryFeature>().Context;

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
