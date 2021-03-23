// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core.Invocation
{
    /// <summary>
    /// Contains extension methods to work with <see cref="IFunctionActivator"/> instances.
    /// </summary>
    public static class FunctionActivatorExtensions
    {
        /// <summary>
        /// Creates an instance of the specified generic type argument, <typeparamref name="T"/>, to be used as the target of the invocation.
        /// </summary>
        /// <typeparam name="T">The type of the instance to create.</typeparam>
        /// <param name="activator">The <see cref="IFunctionActivator"/> instance to use when creating the instance.</param>
        /// <param name="context">The <see cref="FunctionContext"/> for the invocation triggering the instance creation.</param>
        /// <returns>The created instance.</returns>
        public static T? CreateInstance<T>(this IFunctionActivator activator, FunctionContext context)
           where T : class
        {
            if (activator is null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            return activator.CreateInstance(typeof(T), context) as T;
        }
    }
}
