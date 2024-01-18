using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    internal class FunctionsApplicationInsightsOptionsInitializer : IConfigureOptions<FunctionsApplicationInsightsOptions>
    {
        private const string AppInsightsAuthenticationString = "APPLICATIONINSIGHTS_AUTHENTICATION_STRING";

        public void Configure(FunctionsApplicationInsightsOptions options)
        {
            // Read AuthString from environment variable
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AppInsightsAuthenticationString)))
            {
                options.TokenCredentialOptions = TokenCredentialOptions.ParseAuthenticationString(Environment.GetEnvironmentVariable(AppInsightsAuthenticationString));
            }
        }
    }
}
