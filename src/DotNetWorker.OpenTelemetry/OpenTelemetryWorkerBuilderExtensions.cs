// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    public static class OpenTelemetryWorkerBuilderExtensions
    {
        public static IFunctionsWorkerApplicationBuilder EnableBaggagePropagation(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseMiddleware<BaggageMiddleware>();
            return builder;
        }

    }
}
