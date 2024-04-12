// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    public static class ConfigureFunctionsOpenTelemetry
    {   
        public static OpenTelemetryBuilder UseFunctionsWorkerDefaults(this OpenTelemetryBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services
                // Lets the host know that the worker is sending logs to App Insights. The host will now ignore these.
                .Configure<WorkerOptions>(workerOptions => workerOptions.Capabilities["WorkerOpenTelemetryEnabled"] = bool.TrueString);

            builder.ConfigureResource(r =>
            {
                r.AddDetector(new FunctionsResourceDetector());
            });

            return builder;
        }
    }
}
