// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// The base type for worker extension startup implementations.
    /// </summary>
    public abstract class WorkerExtensionStartup
    {
        /// <summary>
        /// Configures the function worker application builder option.
        /// Called once during app startup.
        /// </summary>
        /// <param name="applicationBuilder">The IFunctionsWorkerApplicationBuilder instance.</param>
        public abstract void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder);
    }
}
