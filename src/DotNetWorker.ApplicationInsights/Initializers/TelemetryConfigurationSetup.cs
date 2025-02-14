﻿using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    internal class TelemetryConfigurationSetup : IConfigureOptions<TelemetryConfiguration>
    {
        private const string AppInsightsAuthenticationString = "APPLICATIONINSIGHTS_AUTHENTICATION_STRING";
        private readonly IConfiguration _configuration;
        
        public TelemetryConfigurationSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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
