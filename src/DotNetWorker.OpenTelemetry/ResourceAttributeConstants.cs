using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    internal class ResourceAttributeConstants
    {
        internal const string AttributeCloudProvider = "cloud.provider";
        internal const string AttributeCloudPlatform = "cloud.platform";
        internal const string AttributeCloudRegion = "cloud.region";
        internal const string AttributeCloudResourceId = "cloud.resource.id";        
        internal const string AzureCloudProviderValue = "azure";
        internal const string AzurePlatformValue = "azure_functions";
        internal const string AttributeSDKPrefix = "ai.sdk.prefix";
        internal const string AttributeProcessId = "process.pid";
        internal const string AttributeVersion = "faas.version";
        internal const string SDKPrefix = "azurefunctions";
        internal const string SiteNameEnvVar = "WEBSITE_SITE_NAME";
        internal const string RegionNameEnvVar = "REGION_NAME";
        internal const string ResourceGroupEnvVar = "WEBSITE_RESOURCE_GROUP";
        internal const string OwnerNameEnvVar = "WEBSITE_OWNER_NAME";
    }
}
