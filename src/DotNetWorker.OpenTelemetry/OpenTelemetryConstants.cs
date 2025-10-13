// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    internal class OpenTelemetryConstants
    {
        internal const string AzureCloudProviderValue = "azure";
        internal const string AzurePlatformValue = "azure_functions";
        internal const string SDKPrefix = "dotnetiso";
        internal const string SiteNameEnvVar = "WEBSITE_SITE_NAME";
        internal const string RegionNameEnvVar = "REGION_NAME";
        internal const string ResourceGroupEnvVar = "WEBSITE_RESOURCE_GROUP";
        internal const string OwnerNameEnvVar = "WEBSITE_OWNER_NAME";
        internal const string WorkerSchemaVersion = "1.37.0";
        internal const string WorkerActivitySourceName = "Microsoft.Azure.Functions.Worker";


        // Capability variables
        internal const string WorkerOTelEnabled = "WorkerOpenTelemetryEnabled";
        internal const string WorkerOTelSchemaVersion = "WorkerOpenTelemetrySchemaVersion";
    }
}
