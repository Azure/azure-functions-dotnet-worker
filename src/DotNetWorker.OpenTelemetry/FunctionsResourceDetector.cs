// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using OpenTelemetry.Resources;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{

    public sealed class FunctionsResourceDetector : IResourceDetector
    {  
        internal static readonly IReadOnlyDictionary<string, string> ResourceAttributes = new Dictionary<string, string>(1)
        {
            { ResourceAttributeConstants.AttributeCloudRegion, ResourceAttributeConstants.RegionNameEnvVar },
        };

        /// <inheritdoc/>
        public Resource Detect()
        {
            List<KeyValuePair<string, object>> attributeList = new(5);
            try
            {
                var siteName = Environment.GetEnvironmentVariable(ResourceAttributeConstants.SiteNameEnvVar);
                attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeCloudProvider, ResourceAttributeConstants.AzureCloudProviderValue));
                attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeCloudPlatform, ResourceAttributeConstants.AzurePlatformValue));

                var version = typeof(FunctionsResourceDetector).Assembly.GetName().Version.ToString();
                attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeVersion, version)); 
                if (!string.IsNullOrEmpty(siteName))
                {
                    var azureResourceUri = GetAzureResourceURI(siteName);
                    if (azureResourceUri != null)
                    {
                        attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeCloudResourceId, azureResourceUri));
                    }

                    foreach (var kvp in ResourceAttributes)
                    {
                        var attributeValue = Environment.GetEnvironmentVariable(kvp.Value);
                        if (attributeValue != null)
                        {
                            attributeList.Add(new KeyValuePair<string, object>(kvp.Key, attributeValue));
                        }
                    }
                }
            }
            catch
            {
                // return empty resource.
                return Resource.Empty;
            }

            return new Resource(attributeList);
        }
        private static string? GetAzureResourceURI(string websiteSiteName)
        {
            string? websiteResourceGroup = Environment.GetEnvironmentVariable(ResourceAttributeConstants.ResourceGroupEnvVar);
            string? websiteOwnerName = Environment.GetEnvironmentVariable(ResourceAttributeConstants.OwnerNameEnvVar);

            if (string.IsNullOrEmpty(websiteResourceGroup) || string.IsNullOrEmpty(websiteOwnerName))
            {
                return null;
            }

            int idx = websiteOwnerName.IndexOf("+", StringComparison.Ordinal);
            string subscriptionId = idx > 0 ? websiteOwnerName.Substring(0, idx) : websiteOwnerName;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                return null;
            }

            return $"/subscriptions/{subscriptionId}/resourceGroups/{websiteResourceGroup}/providers/Microsoft.Web/sites/{websiteSiteName}";
        }
    }
}
