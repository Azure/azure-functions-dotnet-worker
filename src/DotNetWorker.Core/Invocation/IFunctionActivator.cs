// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines a service that will be used to create function class instances.
    /// </summary>
    public interface IFunctionActivator
    {
        /// <summary>
        /// Creates an instance of the provided type to be used as the target of the invocation.
        /// </summary>
        /// <param name="instanceType">The <see cref="Type"/> of the instance to create.</param>
        /// <param name="context">The <see cref="FunctionContext"/> for the invocation triggering the instance creation.</param>
        /// <returns>The created instance.</returns>
        object? CreateInstance(Type instanceType, FunctionContext context);
    }
}
