// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class FunctionsTelemetryInitializer : ITelemetryInitializer
    {
        private const string NameKey = "Name";

        private readonly string _sdkVersion;
        private readonly string _roleInstanceName;

        internal FunctionsTelemetryInitializer(string sdkVersion, string roleInstanceName)
        {
            _sdkVersion = sdkVersion;
            _roleInstanceName = roleInstanceName;
        }

        public FunctionsTelemetryInitializer() :
            this(GetSdkVersion(), GetRoleInstanceName())
        {
        }

        private static string GetSdkVersion()
        {
            return "azurefunctions-netiso: " + typeof(FunctionsTelemetryInitializer).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
        }

        private static string GetRoleInstanceName()
        {
            const string ComputerNameKey = "COMPUTERNAME";
            const string WebSiteInstanceIdKey = "WEBSITE_INSTANCE_ID";
            const string ContainerNameKey = "CONTAINER_NAME";

            string? instanceName = Environment.GetEnvironmentVariable(WebSiteInstanceIdKey);
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = Environment.GetEnvironmentVariable(ComputerNameKey);
                if (string.IsNullOrEmpty(instanceName))
                {
                    instanceName = Environment.GetEnvironmentVariable(ContainerNameKey);
                }
            }

            return instanceName ?? Environment.MachineName;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                return;
            }

            telemetry.Context.Cloud.RoleInstance = _roleInstanceName;
            telemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;

            telemetry.Context.Location.Ip ??= "0.0.0.0";

            if (Activity.Current is not null)
            {
                foreach (var tag in Activity.Current.Tags)
                {
                    switch (tag.Key)
                    {
                        case NameKey:
                            telemetry.Context.Operation.Name = tag.Value;
                            continue;
                        default:
                            break;
                    }

                    if (telemetry is ISupportProperties properties && !tag.Key.StartsWith("ai_"))
                    {
                        properties.Properties[tag.Key] = tag.Value;
                    }
                }

            }
        }
    }
}
