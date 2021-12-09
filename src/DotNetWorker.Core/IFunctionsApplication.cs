// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a function application.
    /// </summary>
    internal interface IFunctionsApplication
    {
        /// <summary>
        /// Loads a function definition.
        /// </summary>
        /// <param name="definition">The <see cref="FunctionDefinition"/> to load.</param>
        void LoadFunction(FunctionDefinition definition);

        /// <summary>
        /// Asynchronously invokes an <see cref="FunctionContext"/>.
        /// </summary>
        /// <param name="context">The TContext that the operation will process.</param>
        Task InvokeFunctionAsync(FunctionContext context);

        /// <summary>
        /// Create a <see cref="FunctionContext"/> given a collection of invocation features.
        /// </summary>
        /// <param name="features">A collection of invocation features to be used for creating the <see cref="FunctionContext"/>.</param>
        /// <returns>The created TContext.</returns>
        FunctionContext CreateContext(IInvocationFeatures features);

        /// <summary>
        /// Dispose a given <see cref="FunctionContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/> to be disposed.</param>
        /// <param name="exception">The Exception thrown when processing did not complete successfully, otherwise null.</param>
        void DisposeContext(FunctionContext context, Exception? exception);
    }
}
