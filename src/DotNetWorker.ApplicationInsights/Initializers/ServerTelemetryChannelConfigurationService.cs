// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    /// <summary>
    /// Applies <see cref="FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay" /> to the Application Insights
    /// <see cref="ServerTelemetryChannel" /> on startup. This runs after the container is fully built, so the value is
    /// applied regardless of whether <c>ConfigureFunctionsApplicationInsights</c> was called before or after
    /// <c>AddApplicationInsightsTelemetryWorkerService</c>.
    /// </summary>
    internal class ServerTelemetryChannelConfigurationService : IHostedService
    {
        private static readonly TimeSpan MinTelemetryBufferDelay = TimeSpan.FromSeconds(5);
        private readonly IServiceProvider _serviceProvider;
        private readonly FunctionsApplicationInsightsOptions _options;

        public ServerTelemetryChannelConfigurationService(IServiceProvider serviceProvider, IOptions<FunctionsApplicationInsightsOptions> options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_serviceProvider.GetService<TelemetryConfiguration>()?.TelemetryChannel is ServerTelemetryChannel channel)
            {
                if (_options.MaxTelemetryBufferDelay < MinTelemetryBufferDelay)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay),
                        _options.MaxTelemetryBufferDelay,
                        $"{nameof(FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay)} must be at least {MinTelemetryBufferDelay.TotalSeconds} seconds.");
                }

                channel.MaxTelemetryBufferDelay = _options.MaxTelemetryBufferDelay;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
