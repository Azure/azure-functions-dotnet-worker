// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class FunctionsTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string? _sdkVersion;

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
            string? version = typeof(FunctionsTelemetryInitializer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            return version == null ? null : $"azurefunctions-netiso: {version}";
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is null || _sdkVersion is null)
            {
                return;
            }

            telemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;
        }
    }
}
