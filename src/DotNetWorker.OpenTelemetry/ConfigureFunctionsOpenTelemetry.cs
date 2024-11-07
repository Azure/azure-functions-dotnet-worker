// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    public static class ConfigureFunctionsOpenTelemetry
    {   
        public static IOpenTelemetryBuilder UseFunctionsWorkerDefaults(this IOpenTelemetryBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services
                // Tells the host to no longer emit telemetry on behalf of the worker.
                .Configure<WorkerOptions>(workerOptions => workerOptions.Capabilities["WorkerOpenTelemetryEnabled"] = bool.TrueString);

            builder.ConfigureResource((resourceBuilder) =>
            {
                resourceBuilder.AddDetector(new FunctionsResourceDetector());
            });

            return builder;
        }
    }
}
