// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Tests.Features;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal class TestFunctionContext : FunctionContext, IDisposable
    {
        private readonly FunctionInvocation _invocation;
     
        public TestFunctionContext()
            : this(new TestFunctionDefinition(), new TestFunctionInvocation())
        {
        }

        public TestFunctionContext(IInvocationFeatures features)
            : this(new TestFunctionDefinition(), new TestFunctionInvocation(), features)
        {
        }

        public TestFunctionContext(FunctionDefinition functionDefinition, FunctionInvocation invocation, IInvocationFeatures features = null)
        {
            FunctionDefinition = functionDefinition;
            _invocation = invocation;

            if (features != null)
            {
                Features = features;
            }

            Features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature
            {
                OutputBindingsInfo = new DefaultOutputBindingsInfoProvider().GetBindingsInfo(FunctionDefinition)
            });


            BindingContext = new DefaultBindingContext(this);
        }

        public bool IsDisposed { get; private set; }

        public override IServiceProvider InstanceServices { get; set; }

        public override FunctionDefinition FunctionDefinition { get; }

        public override IDictionary<object, object> Items { get; set; }

        public override IInvocationFeatures Features { get; } = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());

        public override string InvocationId => _invocation.Id;

        public override string FunctionId => _invocation.FunctionId;

        public override TraceContext TraceContext => _invocation.TraceContext;

        public override BindingContext BindingContext { get; }

        public override RetryContext RetryContext => Features.Get<IExecutionRetryFeature>()?.Context;

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
