using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    internal class TelemetryConfigurationSetup : IConfigureOptions<TelemetryConfiguration>
    {
        private const string AppInsightsAuthenticationString = "APPLICATIONINSIGHTS_AUTHENTICATION_STRING";
        private static readonly TimeSpan MinTelemetryBufferDelay = TimeSpan.FromSeconds(5);
        private readonly IConfiguration _configuration;
        private readonly FunctionsApplicationInsightsOptions _options;
        
        public TelemetryConfigurationSetup(IConfiguration configuration, IOptions<FunctionsApplicationInsightsOptions> options)
        {
            _configuration = configuration;
            _options = options.Value;
        }

        public void Configure(TelemetryConfiguration telemetryConfiguration)
        {
            string? authString = GetAuthenticationString();
            if (authString is not null)
            {
                telemetryConfiguration.SetAzureTokenCredential(TokenCredentialOptions.ParseAuthenticationString(authString).CreateTokenCredential());
            }

            if (telemetryConfiguration.TelemetryChannel is ServerTelemetryChannel serverTelemetryChannel)
            {
                if (_options.MaxTelemetryBufferDelay < MinTelemetryBufferDelay)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(FunctionsApplicationInsightsOptions)}.{nameof(FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay)}",
                        _options.MaxTelemetryBufferDelay,
                        $"{nameof(FunctionsApplicationInsightsOptions.MaxTelemetryBufferDelay)} must be at least {MinTelemetryBufferDelay.TotalSeconds} seconds.");
                }

                serverTelemetryChannel.MaxTelemetryBufferDelay = _options.MaxTelemetryBufferDelay;
            }
        }

        private string? GetAuthenticationString()
        {
            if (_configuration is not null && _configuration[AppInsightsAuthenticationString] is { } value and not "")
            {
                return value;
            }

            if (Environment.GetEnvironmentVariable(AppInsightsAuthenticationString) is { } envValue and not "")
            {
                return envValue;
            }

            return null;
        }
    }
}
