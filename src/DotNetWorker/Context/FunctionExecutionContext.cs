// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Logging;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    public abstract class FunctionExecutionContext
    {
        public abstract FunctionInvocation Invocation { get; set; }

        public abstract IServiceProvider InstanceServices { get; set; }

        public abstract FunctionDefinition FunctionDefinition { get; set; }

        public abstract object? InvocationResult { get; set; }

        public abstract InvocationLogger Logger { get; set; }

        // TODO: Double-check previous projects for layout of FunctionInvocation, Bindings, etc
        public abstract IDictionary<string, object> OutputBindings { get; }

        public abstract IDictionary<object, object> Items { get; set; }
    }
}
