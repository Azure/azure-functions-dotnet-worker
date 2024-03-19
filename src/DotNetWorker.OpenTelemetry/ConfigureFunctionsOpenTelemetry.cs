// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    public static class ConfigureFunctionsOpenTelemetry
    {
        public static OpenTelemetryBuilder ConfigureFunctions(this OpenTelemetryBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services
                // Lets the host know that the worker is sending logs to App Insights. The host will now ignore these.
                .Configure<WorkerOptions>(workerOptions => workerOptions.Capabilities["WorkerApplicationInsightsLoggingEnabled"] = bool.TrueString);
            builder
                .ConfigureResource(r =>
                {
                    var assembly = typeof(WorkerOptions).Assembly.GetName();
                    r.AddService(assembly.Name, serviceVersion: assembly.Version?.ToString());
                    r.AddAttributes([
                        new("ai.sdk.prefix", $@"azurefunctionscoretools: {version} "),
                        new("azurefunctionscoretools_version", version),
                        //new("RoleInstanceId", hostOptions?.CurrentValue.InstanceId ?? string.Empty),
                        new("ProcessId", Process.GetCurrentProcess().Id)
                    ]);
                });

            return builder;
        }
    }
}
