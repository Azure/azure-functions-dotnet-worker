// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionExecutionContext : FunctionExecutionContext, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private IServiceScope? _instanceServicesScope;
        private IServiceProvider? _instanceServices;

        public DefaultFunctionExecutionContext(IServiceScopeFactory serviceScopeFactory, FunctionInvocation invocation,
            FunctionDefinition definition)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            Invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
            FunctionDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
            OutputBindings = new Dictionary<string, object>();
        }

        public override FunctionInvocation Invocation { get; set; }

        public override FunctionDefinition FunctionDefinition { get; set; }

        public override object? InvocationResult { get; set; }

        public override InvocationLogger? Logger { get; set; }

        public override IDictionary<string, object> OutputBindings { get; }

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

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

        public virtual void Dispose()
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
