// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type wrapping the result of a function invocation.
    /// </summary>
    public abstract class InvocationResult : InvocationResult<object>
    {
    }

    /// <summary>
    /// A type wrapping the result of a function invocation.
    /// </summary>
    /// <typeparam name="T">The type of invocation result value.</typeparam>
    public abstract class InvocationResult<T>
    {
        /// <summary>
        /// Gets or sets the invocation result value.
        /// </summary>
        public abstract T? Value { get; set; }
    }
}
