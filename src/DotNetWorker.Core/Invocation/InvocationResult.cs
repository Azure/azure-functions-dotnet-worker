// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type wrapping the result of a function invocation.
    /// </summary>
    /// <typeparam name="T">The type of invocation result value.</typeparam>
    public sealed class InvocationResult<T>
    {
        internal InvocationResult(FunctionContext functionContext, T? value)
        {
            _functionContext = functionContext;
            _value = value;
        }

        private readonly FunctionContext _functionContext;
        private T? _value;

        /// <summary>
        /// Gets or sets the invocation result value.
        /// </summary>
        public T? Value
        {
            get => _value;
            set
            {
                _value = value;
                _functionContext.GetBindings().InvocationResult = value;
            }
        }
    }
}
