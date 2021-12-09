// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class TestWorkerApplicationBuilderExtensions
    {
        public static IFunctionsWorkerApplicationBuilder UseTestWorker(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.AddTestWorker();

            return builder;
        }
    }
}
