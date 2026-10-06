// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Options for configuring the Functions Application Insights integration.
    /// </summary>
    public class FunctionsApplicationInsightsOptions
    {
        /// <summary>
        /// Gets or sets the maximum time telemetry is buffered by the Application Insights
        /// <c>ServerTelemetryChannel</c> before it is sent to the ingestion endpoint. Lowering this value reduces the
        /// delay before telemetry becomes visible, at the cost of more frequent network calls. The default value is
        /// 8 seconds and the minimum is 5 seconds. This is only applied when the configured telemetry channel is a
        /// <c>ServerTelemetryChannel</c>.
        /// </summary>
        public TimeSpan MaxTelemetryBufferDelay { get; set; } = TimeSpan.FromSeconds(8);
    }
}
