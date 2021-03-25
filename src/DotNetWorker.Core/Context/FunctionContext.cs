// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Encapsulates the information about a function execution.
    /// </summary>
    public abstract class FunctionContext
    {
        /// <summary>
        /// Gets the invocation ID.
        /// This identifier is unique to an invocation.
        /// </summary>
        public abstract string InvocationId { get; }

        /// <summary>
        /// Gets the function ID, typically assigned by the host.
        /// This identifier is unique to a function and stable across invocations.
        /// </summary>
        public abstract string FunctionId { get; }

        /// <summary>
        /// Gets the distributed tracing context.
        /// </summary>
        public abstract TraceContext TraceContext { get; }

        /// <summary>
        /// Gets the binding context for the current function invocation.
        /// This context is used to retrieve binding data.
        /// </summary>
        public abstract BindingContext BindingContext { get; }

        /// <summary>
        /// Gets the retry context containing information about retry acvitity for the event that triggered
        /// the current function invocation.
        /// </summary>
        public abstract RetryContext RetryContext { get; }

        /// <summary>
        /// Gets or sets the <see cref="IServiceProvider"/> that provides access to this execution's services.
        /// </summary>
        public abstract IServiceProvider InstanceServices { get; set; }

        /// <summary>
        /// Gets the <see cref="FunctionDefinition"/> that describes the function being executed.
        /// </summary>
        public abstract FunctionDefinition FunctionDefinition { get; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this invocation.
        /// </summary>
        public abstract IDictionary<object, object> Items { get; set; }

        /// <summary>
        /// Gets a collection containing the features supported by this context.
        /// </summary>
        public abstract IInvocationFeatures Features { get; }
    }
}
