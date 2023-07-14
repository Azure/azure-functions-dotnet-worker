// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    internal class FunctionsTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string? _sdkVersion;
        private static readonly IDictionary<string, string> _mappings = new Dictionary<string, string>()
        {
            { "faas.execution", "InvocationId" }, // used by worker ActivitySource
            { "AzureFunctions_InvocationId", "InvocationId" } // used by log scope
        };

        internal FunctionsTelemetryInitializer(string? sdkVersion)
        {
            _sdkVersion = sdkVersion;
        }

        public FunctionsTelemetryInitializer() :
            this(GetSdkVersion())
        {
        }

        private static string? GetSdkVersion()
        {
            var version = typeof(FunctionsTelemetryInitializer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            return version == null ? null : $"azurefunctions-netiso: {version}";
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is null)
            {
                return;
            }

            if (telemetry is not null)
            {
                telemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;
            }

            if (telemetry is ISupportProperties supportProperties)
            {
                CopyWellKnownProperties(supportProperties);
            }
        }

        // For parity with how the Functions host writes to App Insights, make sure we translate some
        // well-known worker keys into the ones used by the host.
        internal static void CopyWellKnownProperties(ISupportProperties supportProperties)
        {
            foreach (var mapping in _mappings)
            {
                if (supportProperties.Properties.TryGetValue(mapping.Key, out string propValue))
                {
                    supportProperties.Properties[mapping.Value] = propValue;
                }
            }
        }
    }
}
