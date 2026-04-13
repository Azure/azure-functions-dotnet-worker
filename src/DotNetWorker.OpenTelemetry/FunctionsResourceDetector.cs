// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Resources;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{

    public sealed class FunctionsResourceDetector : IResourceDetector
    {
        private static readonly string s_assemblyVersion =
            typeof(FunctionsResourceDetector).Assembly.GetName().Version?.ToString() ?? "unknown";

        private static readonly int s_processId = Process.GetCurrentProcess().Id;

        public Resource Detect()
        {
            try
            {
                var attributes = new List<KeyValuePair<string, object>>(capacity: 10)
                {
                    new(ResourceSemanticConventions.AISDKPrefix, $"{OpenTelemetryConstants.SDKPrefix}:{s_assemblyVersion}"),
                    new(ResourceSemanticConventions.ProcessId, s_processId)
                };

                string? siteName = Environment.GetEnvironmentVariable(OpenTelemetryConstants.SiteNameEnvVar);
                string? resourceAttributes = Environment.GetEnvironmentVariable(OpenTelemetryConstants.ResourceAttributeEnvVar);

                // Priority: OTEL_SERVICE_NAME > OTEL_RESOURCE_ATTRIBUTES[service.name] > WEBSITE_SITE_NAME > AssemblyName
                if (!IsServiceNameConfigured(resourceAttributes))
                {
                    attributes.Add(new(ResourceSemanticConventions.ServiceName,
                        siteName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown"));
                }

                // Priority: OTEL_RESOURCE_ATTRIBUTES[service.version] > AssemblyVersion
                // OTel decided to not have OTEL_SERVICE_VERSION, so we only check OTEL_RESOURCE_ATTRIBUTES.
                // https://github.com/open-telemetry/semantic-conventions/issues/2669
                if (!IsResourceAttributeConfigured(ResourceSemanticConventions.ServiceVersion, resourceAttributes))
                {
                    attributes.Add(new(ResourceSemanticConventions.ServiceVersion,
                        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown"));
                }

                // Add these attributes only if running in Azure.
                if (!string.IsNullOrEmpty(siteName))
                {
                    attributes.Add(new(ResourceSemanticConventions.CloudProvider, OpenTelemetryConstants.AzureCloudProviderValue));
                    attributes.Add(new(ResourceSemanticConventions.CloudPlatform, OpenTelemetryConstants.AzurePlatformValue));

                    if (Environment.GetEnvironmentVariable(OpenTelemetryConstants.RegionNameEnvVar) is { Length: > 0 } region)
                    {
                        attributes.Add(new(ResourceSemanticConventions.CloudRegion, region));
                    }

                    if (GetAzureResourceUri(siteName!) is { } uri)
                    {
                        attributes.Add(new(ResourceSemanticConventions.CloudResourceId, uri));
                    }

                    if (Environment.GetEnvironmentVariable(OpenTelemetryConstants.SlotNameEnvVar) is { Length: > 0 } slot)
                    {
                        attributes.Add(new(ResourceSemanticConventions.DeploymentEnvironmentName, slot));
                    }

                    if (Environment.GetEnvironmentVariable(OpenTelemetryConstants.SiteUpdateIdEnvVar) is { Length: > 0 } siteUpdateId)
                    {
                        attributes.Add(new(ResourceSemanticConventions.SiteUpdateId, siteUpdateId));
                    }
                }

                return new Resource(attributes);
            }
            catch
            {
                // return empty resource.
                return Resource.Empty;
            }
        }

        private static bool IsServiceNameConfigured(string? resourceAttributes)
        {
            if (Environment.GetEnvironmentVariable(OpenTelemetryConstants.ServiceNameEnvVar) is { Length: > 0 })
            {
                return true;
            }

            return IsResourceAttributeConfigured(ResourceSemanticConventions.ServiceName, resourceAttributes);
        }

        private static bool IsResourceAttributeConfigured(string key, string? resourceAttributes)
        {
            if (resourceAttributes is not { Length: > 2 })
            {
                return false;
            }

            // TODO: Replace manual parsing with MemoryExtensions.Split when we upgrade to .NET 10.
            var remaining = resourceAttributes.AsSpan();

            while (remaining.Length > 0)
            {
                var commaIndex = remaining.IndexOf(',');
                var segment = commaIndex >= 0
                    ? remaining[..commaIndex]
                    : remaining;

                var trimmed = segment.Trim();
                var equalsIndex = trimmed.IndexOf('=');

                if (equalsIndex > 0)
                {
                    var attributeKey = trimmed[..equalsIndex];
                    if (attributeKey.Equals(key, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                if (commaIndex < 0)
                {
                    break;
                }

                remaining = remaining[(commaIndex + 1)..];
            }

            return false;
        }

        private static string? GetAzureResourceUri(string siteName)
        {
            var resourceGroup = Environment.GetEnvironmentVariable(OpenTelemetryConstants.ResourceGroupEnvVar);
            var owner = Environment.GetEnvironmentVariable(OpenTelemetryConstants.OwnerNameEnvVar);

            if (string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(owner))
            {
                return null;
            }

            // owner format: "{subscriptionId}+{something}"
            var span = owner.AsSpan();
            var plusIndex = span.IndexOf('+');

            var subscriptionId = plusIndex > 0
                ? span[..plusIndex].ToString()
                : owner;

            return $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/{siteName}";
        }
    }
}
