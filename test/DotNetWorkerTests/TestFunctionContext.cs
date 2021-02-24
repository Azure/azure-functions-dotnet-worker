// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal class TestFunctionContext : FunctionContext, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public override IServiceProvider InstanceServices { get; set; }

        public override FunctionDefinition FunctionDefinition { get; set; }

        public override object InvocationResult { get; set; }

        public override IDictionary<object, object> Items { get; set; }

        public override FunctionInvocation Invocation { get; set; }

        public override IDictionary<string, object> OutputBindings { get; } = new Dictionary<string, object>();

        public override IInvocationFeatures Features { get; } = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
