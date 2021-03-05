// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Middleware
{
    /// <summary>
    /// Represents a middleware to be used in the worker execution pipeline.
    /// </summary>
    public interface IFunctionsWorkerMiddleware
    {
        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/> for the current invocation.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous invocation.</returns>
        Task Invoke(FunctionContext context, FunctionExecutionDelegate next);
    }
}
