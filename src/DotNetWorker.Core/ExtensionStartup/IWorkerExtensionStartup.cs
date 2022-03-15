// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A type representing the contract for worker extension startup implementation.
    /// </summary>
    public interface IWorkerExtensionStartup
    {
        /// <summary>
        /// Configures the function worker application builder option.
        /// </summary>
        /// <param name="applicationBuilder">The IFunctionsWorkerApplicationBuilder instance.</param>
        void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder);
    }
}
