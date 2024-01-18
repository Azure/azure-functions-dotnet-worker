using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    internal class TelemetryConfigurationSetup : IConfigureOptions<TelemetryConfiguration>
    {
        private readonly IConfiguration _configuration;
        public TelemetryConfigurationSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        private const string AppInsightsAuthenticationString = "APPLICATIONINSIGHTS_AUTHENTICATION_STRING";

        public void Configure(TelemetryConfiguration telemetryConfiguration)
        {
            string? authString = GetAuthenticationString();
            if (authString is not null)
            {
                telemetryConfiguration.SetAzureTokenCredential(TokenCredentialOptions.ParseAuthenticationString(authString).CreateTokenCredential());
            }
        }

        private string? GetAuthenticationString()
        {
            if (_configuration is not null && _configuration[AppInsightsAuthenticationString] is string value && value is not "")
            {
                return value;
            }
            else if (Environment.GetEnvironmentVariable(AppInsightsAuthenticationString) is string envValue && envValue is not "")
            {
                return envValue;
            }
            else
            {
                return null;
            }
        }
    }
}
