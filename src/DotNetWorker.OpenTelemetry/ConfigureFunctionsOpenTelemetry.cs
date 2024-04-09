﻿// Copyright (c) .NET Foundation. All rights reserved.
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
                string version = typeof(ConfigureFunctionsOpenTelemetry).Assembly.GetName().Version.ToString();
                ConfigureResourceAttributes(r, version);
            });

            return builder;
        }

        private static string GetServiceName() => Environment.GetEnvironmentVariable(ResourceAttributeConstants.SiteNameEnvVar) ?? Assembly.GetEntryAssembly()?.GetName().Name ?? ResourceAttributeConstants.SDKPrefix;

        private static void ConfigureResourceAttributes(ResourceBuilder r, string version)
        {
            r.AddService(GetServiceName(), serviceVersion: version)
                .AddDetector(new FunctionsResourceDetector())
                .AddAttributes(
                 [
                    new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeSDKPrefix, $"{ResourceAttributeConstants.SDKPrefix}: {version}"),
                    new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeProcessId, Process.GetCurrentProcess().Id.ToString())
                 ]);
        }
    }
}
