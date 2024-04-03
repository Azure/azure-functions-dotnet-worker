// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

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
            builder
                .ConfigureResource(r =>
                {
                    string serviceName = Environment.GetEnvironmentVariable(ResourceAttributeConstants.SiteNameEnvVar) ?? "azureFunctions";
                    string version = typeof(ConfigureFunctionsOpenTelemetry).Assembly.GetName().Version.ToString();
                    r.AddService(serviceName, serviceVersion: version);

                    // Set the AI SDK to a key so we know all the telemetry came from the Functions Host
                    // NOTE: This ties to \azure-sdk-for-net\sdk\monitor\Azure.Monitor.OpenTelemetry.Exporter\src\Internals\ResourceExtensions.cs :: AiSdkPrefixKey used in CreateAzureMonitorResource()
                    r.AddAttributes([
                        new(ResourceAttributeConstants.AttributeSDKPrefix, $@"{ResourceAttributeConstants.SDKPrefix}: {version}"),
                        new(ResourceAttributeConstants.AttributeProcessId, Process.GetCurrentProcess().Id)
                    ]);
                });

            return builder;
        }
    }
}
