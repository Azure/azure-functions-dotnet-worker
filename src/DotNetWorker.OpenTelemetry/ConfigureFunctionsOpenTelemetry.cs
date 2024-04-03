// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    public static class ConfigureFunctionsOpenTelemetry
    {
        private const string DefaultServiceName = "dotnetiso";
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
                string version = typeof(ConfigureFunctionsOpenTelemetry).Assembly.GetName().Version.ToString();
                ConfigureResourceAttributes(r, version);
            });

            return builder;
        }

        private static string GetServiceName()
        {
            return Environment.GetEnvironmentVariable(ResourceAttributeConstants.SiteNameEnvVar) ?? DefaultServiceName;
        }

        private static void ConfigureResourceAttributes(ResourceBuilder r, string version)
        {
            r.AddService(GetServiceName(), serviceVersion: version)
             .AddAttributes(new[]
             {
                new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeSDKPrefix, $"{ResourceAttributeConstants.SDKPrefix}: {version}"),
                new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeProcessId, Process.GetCurrentProcess().Id.ToString())
             });
        }
    }
}
