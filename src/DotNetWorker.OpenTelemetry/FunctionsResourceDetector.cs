using System;
using System.Collections.Generic;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{

    public sealed class FunctionsResourceDetector : IResourceDetector
    {
        internal static readonly IReadOnlyDictionary<string, string> AppServiceResourceAttributes = new Dictionary<string, string>
        {
            { ResourceAttributeConstants.AttributeCloudRegion, ResourceAttributeConstants.AppServiceRegionNameEnvVar },
            //{ ResourceSemanticConventions.AttributeDeploymentEnvironment, ResourceAttributeConstants.AppServiceSlotNameEnvVar },
            //{ ResourceSemanticConventions.AttributeHostId, ResourceAttributeConstants.AppServiceHostNameEnvVar },
            //{ ResourceSemanticConventions.AttributeServiceInstance, ResourceAttributeConstants.AppServiceInstanceIdEnvVar },
            { ResourceAttributeConstants.AzureAppServiceStamp, ResourceAttributeConstants.AppServiceStampNameEnvVar },
        };

        /// <inheritdoc/>
        public Resource Detect()
        {
            List<KeyValuePair<string, object>> attributeList = new();

            try
            {
                var websiteSiteName = Environment.GetEnvironmentVariable(ResourceAttributeConstants.AppServiceSiteNameEnvVar);
                attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeCloudProvider, ResourceAttributeConstants.AzureCloudProviderValue));
                attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeCloudPlatform, ResourceAttributeConstants.AzureAppServicePlatformValue));

                if (websiteSiteName != null)
                {
                    //attributeList.Add(new KeyValuePair<string, object>(ResourceSemanticConventions.AttributeServiceName, websiteSiteName));
                    

                    var azureResourceUri = GetAzureResourceURI(websiteSiteName);
                    if (azureResourceUri != null)
                    {
                        attributeList.Add(new KeyValuePair<string, object>(ResourceAttributeConstants.AttributeCloudResourceId, azureResourceUri));
                    }

                    foreach (var kvp in AppServiceResourceAttributes)
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
                // TODO: log exception.
                return Resource.Empty;
            }

            return new Resource(attributeList);
        }

        private static string? GetAzureResourceURI(string websiteSiteName)
        {
            string? websiteResourceGroup = Environment.GetEnvironmentVariable(ResourceAttributeConstants.AppServiceResourceGroupEnvVar);
            string websiteOwnerName = Environment.GetEnvironmentVariable(ResourceAttributeConstants.AppServiceOwnerNameEnvVar) ?? string.Empty;

#if NET6_0_OR_GREATER
        int idx = websiteOwnerName.IndexOf('+', StringComparison.Ordinal);
#else
            int idx = websiteOwnerName.IndexOf("+", StringComparison.Ordinal);
#endif
            string subscriptionId = idx > 0 ? websiteOwnerName.Substring(0, idx) : websiteOwnerName;

            if (string.IsNullOrEmpty(websiteResourceGroup) || string.IsNullOrEmpty(subscriptionId))
            {
                return null;
            }

            return $"/subscriptions/{subscriptionId}/resourceGroups/{websiteResourceGroup}/providers/Microsoft.Web/sites/{websiteSiteName}";
        }

    }
}
