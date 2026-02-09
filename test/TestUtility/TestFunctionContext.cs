// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestAsyncFunctionContext : TestFunctionContext, IAsyncDisposable
    {
        public TestAsyncFunctionContext()
            : base(new TestFunctionDefinition(), new TestFunctionInvocation(), CancellationToken.None)
        {
        }
        public TestAsyncFunctionContext(IInvocationFeatures features) : base(features)
        {
        }

        public bool IsAsyncDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsAsyncDisposed = true;
            return default;
        }
    }

    public class TestFunctionContext : FunctionContext, IDisposable
    {
        private readonly FunctionInvocation _invocation;
        private readonly CancellationToken _cancellationToken;

        public TestFunctionContext()
            : this(new TestFunctionDefinition(), new TestFunctionInvocation(), CancellationToken.None)
        {
        }

        public TestFunctionContext(IInvocationFeatures features)
            : this(new TestFunctionDefinition(), new TestFunctionInvocation(), CancellationToken.None, features)
        {
        }

        public TestFunctionContext(IInvocationFeatures features, CancellationToken token)
            : this(new TestFunctionDefinition(), new TestFunctionInvocation(), token, features)
        {
        }

        public TestFunctionContext(FunctionDefinition functionDefinition, FunctionInvocation invocation)
            : this(functionDefinition, invocation, CancellationToken.None)
        {
        }

        public TestFunctionContext(FunctionDefinition functionDefinition, FunctionInvocation invocation, CancellationToken cancellationToken, IInvocationFeatures features = null, IServiceProvider serviceProvider = null)
        {
            FunctionDefinition = functionDefinition;
            _invocation = invocation;
            _cancellationToken = cancellationToken;

            if (features != null)
            {
                Features = features;
            }

            if (Features.Get<IFunctionBindingsFeature>() == null)
            {
                Features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature
                {
                    OutputBindingsInfo = new DefaultOutputBindingsInfoProvider().GetBindingsInfo(FunctionDefinition)
                });
            }

            InstanceServices = serviceProvider;
            BindingContext = new DefaultBindingContext(this);
        }

        public bool IsDisposed { get; private set; }

        public override IServiceProvider InstanceServices { get; set; }

        public override FunctionDefinition FunctionDefinition { get; }

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IInvocationFeatures Features { get; } = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());

        public override string InvocationId => _invocation.Id;

        public override string FunctionId => _invocation.FunctionId;

        public override TraceContext TraceContext => _invocation.TraceContext;

        public override BindingContext BindingContext { get; }

        public override RetryContext RetryContext => Features.Get<IExecutionRetryFeature>()?.Context;

        public override CancellationToken CancellationToken => _cancellationToken;

        public static TestFunctionContext Create(FunctionDefinition functionDefinition = null,
            FunctionInvocation functionInvocation = null, ObjectSerializer serializer = null)
        {
            functionDefinition ??= new TestFunctionDefinition();
            functionInvocation ??= new TestFunctionInvocation();

            var context = new TestFunctionContext(functionDefinition, functionInvocation);

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddFunctionsWorkerDefaults();

            if (serializer != null)
            {
                services.Configure<WorkerOptions>(c =>
                {
                    c.Serializer = serializer;
                });
            }

            context.InstanceServices = services.BuildServiceProvider()
                .CreateScope().ServiceProvider;

            return context;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
