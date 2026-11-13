// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    /// <summary>
    /// Applies <see cref="FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay" /> to the Application Insights
    /// <see cref="ServerTelemetryChannel" />. This participates in the standard Application Insights options pipeline as
    /// an <see cref="IConfigureOptions{TelemetryConfiguration}" />. The channel is resolved from the container (the same
    /// singleton the SDK assigns to <see cref="TelemetryConfiguration.TelemetryChannel" />) rather than read from the
    /// <see cref="TelemetryConfiguration" /> instance, so the value is applied regardless of whether
    /// <c>ConfigureFunctionsApplicationInsights</c> was called before or after
    /// <c>AddApplicationInsightsTelemetryWorkerService</c>.
    /// </summary>
    internal sealed class ConfigureServerTelemetryChannel : IConfigureOptions<TelemetryConfiguration>
    {
        private static readonly TimeSpan MinTelemetryBufferDelay = TimeSpan.FromSeconds(5);
        private readonly ITelemetryChannel _channel;
        private readonly FunctionsApplicationInsightsOptions _options;

        public ConfigureServerTelemetryChannel(ITelemetryChannel channel, IOptions<FunctionsApplicationInsightsOptions> options)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public void Configure(TelemetryConfiguration configuration)
        {
            if (_channel is ServerTelemetryChannel serverTelemetryChannel)
            {
                if (_options.MaxTelemetryBufferDelay < MinTelemetryBufferDelay)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay),
                        _options.MaxTelemetryBufferDelay,
                        $"{nameof(FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay)} must be at least {MinTelemetryBufferDelay.TotalSeconds} seconds.");
                }

                serverTelemetryChannel.MaxTelemetryBufferDelay = _options.MaxTelemetryBufferDelay;
            }
        }
    }
}
