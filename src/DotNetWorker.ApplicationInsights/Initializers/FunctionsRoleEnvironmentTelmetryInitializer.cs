// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    // This class was taken largely from https://raw.githubusercontent.com/Microsoft/ApplicationInsights-dotnet-server/91016d62f3181e10d4cf589ef8fd64dadb6b54a2/Src/WindowsServer/WindowsServer.Shared/AzureWebAppRoleEnvironmentTelemetryInitializer.cs, 
    // but refactored so that it did not use WEBSITE_HOSTNAME, which is determined to be unreliable for functions during slot swaps.

    /// <summary>
    /// A telemetry initializer that will gather Azure Web App Role Environment context information.
    /// </summary>    
    internal class FunctionsRoleEnvironmentTelemetryInitializer : ITelemetryInitializer
    {
        internal const string WebAppSuffix = ".azurewebsites.net";

        private readonly ConcurrentDictionary<string, string> _siteNodeNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly IOptionsMonitor<AppServiceOptions> _appServiceOptions;

        public FunctionsRoleEnvironmentTelemetryInitializer(IOptionsMonitor<AppServiceOptions> appServiceOptions)
        {
            _appServiceOptions = appServiceOptions;
        }

        /// <summary>
        /// Initializes <see cref="ITelemetry" /> device context.
        /// </summary>
        /// <param name="telemetry">The telemetry to initialize.</param>
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                return;
            }

            var options = _appServiceOptions.CurrentValue;

            if (!string.IsNullOrEmpty(options.AzureWebsiteCloudRoleName))
            {
                telemetry.Context.Cloud.RoleName = options.AzureWebsiteCloudRoleName;
            }
            else
            {
                telemetry.Context.Cloud.RoleName = options.AzureWebsiteSlotName;
            }

            var internalContext = telemetry.Context.GetInternalContext();
            if (!string.IsNullOrEmpty(options.AzureWebsiteSlotName))
            {
                internalContext.NodeName = _siteNodeNames.GetOrAdd(options.AzureWebsiteSlotName!, p =>
                {
                    // maintain previous behavior of node having the full url
                    return p += WebAppSuffix;
                });
            }
        }
    }
}

