// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{
    internal class FunctionsTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _sdkVersion;
        private readonly string _roleInstanceName;

        public FunctionsTelemetryInitializer(ISdkVersionProvider versionProvider, IRoleInstanceProvider roleInstanceProvider)
        {
            if (versionProvider == null)
            {
                throw new ArgumentNullException(nameof(versionProvider));
            }

            if (roleInstanceProvider == null)
            {
                throw new ArgumentNullException(nameof(roleInstanceProvider));
            }

            _sdkVersion = versionProvider.GetSdkVersion();
            _roleInstanceName = roleInstanceProvider.GetRoleInstanceName();
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                return;
            }

            telemetry.Context.Cloud.RoleInstance = _roleInstanceName;
            telemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;

            if (Activity.Current is not null)
            {
                foreach (var tag in Activity.Current.Tags)
                {
                    switch (tag.Key)
                    {
                        case LogConstants.NameKey:
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
