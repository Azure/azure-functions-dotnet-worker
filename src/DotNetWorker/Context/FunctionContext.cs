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
        /// Gets the <see cref="FunctionInvocation"/> instance containing information about the invocation.
        /// </summary>
        public abstract FunctionInvocation Invocation { get; }

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
