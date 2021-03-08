// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class GrpcWorkerApplicationBuilderExtensions
    {
        public static IFunctionsWorkerApplicationBuilder UseGrpc(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.AddGrpc();

            return builder;
        }
    }
}
