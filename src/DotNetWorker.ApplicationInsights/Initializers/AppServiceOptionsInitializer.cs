using System;
using Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;
using Microsoft.Extensions.Options;

internal class AppServiceOptionsInitializer : IConfigureOptions<AppServiceOptions>
{
    internal const string AzureWebsiteName = "WEBSITE_SITE_NAME";
    internal const string AzureWebsiteSlotName = "WEBSITE_SLOT_NAME";
    internal const string AzureWebsiteCloudRoleName = "WEBSITE_CLOUD_ROLENAME";
    internal const string DefaultProductionSlotName = "production";

    internal static string[] EnvironmentVariablesToMonitor = [AzureWebsiteName, AzureWebsiteSlotName, AzureWebsiteCloudRoleName];

    public void Configure(AppServiceOptions options)
    {
        options.AzureWebsiteName = Environment.GetEnvironmentVariable(AzureWebsiteName);
        options.AzureWebsiteCloudRoleName = Environment.GetEnvironmentVariable(AzureWebsiteCloudRoleName);

        // Compute the slot name by appending non-production slot to the site name (i.e. mysite-staging)
        string slotName = Environment.GetEnvironmentVariable(AzureWebsiteSlotName);
        options.AzureWebsiteSlotName = GetAzureWebsiteUniqueSlotName(options.AzureWebsiteName, slotName);
    }

    /// <summary>
    /// Gets a value that uniquely identifies the site and slot.
    /// </summary>
    private static string? GetAzureWebsiteUniqueSlotName(string? websiteName, string? slotName)
    {
        if (!string.IsNullOrEmpty(slotName) &&
            !string.Equals(slotName, DefaultProductionSlotName, StringComparison.OrdinalIgnoreCase))
        {
            websiteName += $"-{slotName}";
        }

        return websiteName?.ToLowerInvariant();
    }
}
