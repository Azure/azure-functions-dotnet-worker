// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type wrapping the result of a function invocation.
    /// </summary>
    internal sealed class DefaultInvocationResult : InvocationResult
    {
        private readonly FunctionContext _functionContext;
        private object? _value;

        internal DefaultInvocationResult(FunctionContext functionContext, object? value)
        {
            _functionContext = functionContext;
            _value = value;
        }
        /// <inheritdoc/>
        public override object? Value
        {
            get => _value;
            set
            {
                _value = value;
                _functionContext.GetBindings().InvocationResult = value;
            }
        }
    }

    /// <summary>
    /// A type wrapping the result of a function invocation.
    /// </summary>
    /// <typeparam name="T">The type of invocation result value.</typeparam>
    internal sealed class DefaultInvocationResult<T> : InvocationResult<T>
    {
        private readonly FunctionContext _functionContext;
        private T? _value;

        internal DefaultInvocationResult(FunctionContext functionContext, T? value)
        {
            _functionContext = functionContext;
            _value = value;
        }

        /// <inheritdoc/>
        public override T? Value
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
