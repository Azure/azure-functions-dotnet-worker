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
        public Resource Detect()
        {
            List<KeyValuePair<string, object>> attributeList = new(8);
            try
            {
                string serviceName = Environment.GetEnvironmentVariable(OpenTelemetryConstants.SiteNameEnvVar);
                string version = Assembly.GetEntryAssembly().GetName().Version.ToString();

                attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.ServiceVersion, version));
                attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.AISDKPrefix, 
                    $@"{OpenTelemetryConstants.SDKPrefix}:{typeof(FunctionsResourceDetector).Assembly.GetName().Version}"));
                using var process = Process.GetCurrentProcess();
                attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.ProcessId, process.Id));

                // Add these attributes only if running in Azure.
                if (!string.IsNullOrEmpty(serviceName))
                {
                    attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.ServiceName, serviceName));
                    attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.CloudProvider, OpenTelemetryConstants.AzureCloudProviderValue));
                    attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.CloudPlatform, OpenTelemetryConstants.AzurePlatformValue));

                    string region = Environment.GetEnvironmentVariable(OpenTelemetryConstants.RegionNameEnvVar);
                    if (!string.IsNullOrEmpty(region))
                    {
                        attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.CloudRegion, region));
                    }

                    var azureResourceUri = GetAzureResourceURI(serviceName);
                    if (!string.IsNullOrEmpty(azureResourceUri))
                    {
                        attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.CloudResourceId, azureResourceUri));
                    }
                }
                else
                {
                    attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.ServiceName, Assembly.GetEntryAssembly().GetName().Name));
                }
            }
            catch
            {
                // return empty resource.
                return Resource.Empty;
            }
            return new Resource(attributeList);
        }

        private static string GetAzureResourceURI(string websiteSiteName)
        {
            string websiteResourceGroup = Environment.GetEnvironmentVariable(OpenTelemetryConstants.ResourceGroupEnvVar);
            string websiteOwnerName = Environment.GetEnvironmentVariable(OpenTelemetryConstants.OwnerNameEnvVar) ?? string.Empty;
            int idx = websiteOwnerName.IndexOf("+", StringComparison.Ordinal);
            string subscriptionId = idx > 0 ? websiteOwnerName.Substring(0, idx) : websiteOwnerName;

            if (string.IsNullOrEmpty(websiteResourceGroup) || string.IsNullOrEmpty(subscriptionId))
            {
                return string.Empty;
            }

            return $"/subscriptions/{subscriptionId}/resourceGroups/{websiteResourceGroup}/providers/Microsoft.Web/sites/{websiteSiteName}";
        }
    }
}
