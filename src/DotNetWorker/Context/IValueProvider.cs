// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Context
{
    /// <summary>
    /// Represents an object that provides binding data resolution. 
    /// </summary>
    public interface IValueProvider
    {
        /// <summary>
        /// Resolves a value based on the binding name.
        /// </summary>
        /// <param name="name">The name or identifier for the binding.</param>
        /// <param name="functionContext">The <see cref="FunctionContext"/> for the invocation triggering the resolution.</param>
        /// <returns></returns>
        object? GetValue(string name, FunctionContext functionContext);
    }
}
