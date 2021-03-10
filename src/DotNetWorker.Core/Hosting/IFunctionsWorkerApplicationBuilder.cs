// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a builder for a Functions Worker Application.
    /// </summary>
    public interface IFunctionsWorkerApplicationBuilder
    {
        /// <summary>
        /// The collection of services for the current <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Registers a custom middleware in the worker's invocation pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to register.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chanining.</returns>
        IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);
    }
}
