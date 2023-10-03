// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc
{
    /// <summary>
    /// <see cref="IServiceCollection" /> extensions for RPC.
    /// </summary>
    public static partial class RpcServiceCollectionExtensions
    {
        /// <summary>
        /// Adds requires types for Functions host and worker RPC communication. Extensions that want to use RPC are
        /// expected to call this before configuring their RPC clients.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The original service collection for call chaining.</returns>
        public static IServiceCollection AddWorkerRpc(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions<FunctionsGrpcOptions>()
                .Validate(options => options.CallInvoker is not null, "gRPC CallInvoker must not be null.");
            ConfigureCallInvoker(services);
            return services;
        }
    }
}
