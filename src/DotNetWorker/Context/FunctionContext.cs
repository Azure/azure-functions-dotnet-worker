// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Context;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Encapsulates the information about a function execution.
    /// </summary>
    public abstract class FunctionContext
    {
        /// <summary>
        /// Gets or sets the <see cref="FunctionInvocation"/> instance containing information about the invocation.
        /// </summary>
        public abstract FunctionInvocation Invocation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IServiceProvider"/> that provides access to this execution's services.
        /// </summary>
        public abstract IServiceProvider InstanceServices { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FunctionDefinition"/> that describes the function being executed.
        /// </summary>
        public abstract FunctionDefinition FunctionDefinition { get; set; }

        /// <summary>
        /// Gets or sets the result of the invocation.
        /// </summary>
        public abstract object? InvocationResult { get; set; }

        // TODO: Double-check previous projects for layout of FunctionInvocation, Bindings, 
        /// <summary>
        /// Gets or sets a key/value collection with results for defined output bindings.
        /// </summary>
        public abstract IDictionary<string, object> OutputBindings { get; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this invocation.
        /// </summary>
        public abstract IDictionary<object, object> Items { get; set; }
    }
}
