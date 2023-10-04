// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents an interface for auto-configuration of an <see cref="IHostBuilder"/>.
    /// Classes implementing this interface provide a method to configure an <see cref="IHostBuilder"/>.
    /// </summary>
    public interface IAutoConfigureStartup
    {
        /// <summary>
        /// Configures an <see cref="IHostBuilder"/> instance for startup.
        /// Implementing classes define the configuration logic within this method.
        /// This method will be invoked during the bootstrapping process of the function app.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> instance to configure.</param>
        public void Configure(IHostBuilder hostBuilder);
    }
}
