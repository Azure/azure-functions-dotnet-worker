using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    internal static class ResourceSemanticConventions
    {
        // Service
        internal const string ServiceName = "service.name";
        internal const string ServiceVersion = "service.version";

        // Cloud
        internal const string CloudProvider = "cloud.provider";
        internal const string CloudPlatform = "cloud.platform";
        internal const string CloudRegion = "cloud.region";
        internal const string CloudResourceId = "cloud.resource_id";
        
        // Process
        internal const string ProcessId = "process.pid";

        // AI
        internal const string AISDKPrefix = "ai.sdk.prefix";
    }
}
