namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;

internal class AppServiceOptions
{
    public string? AzureWebsiteName { get; set; }

    public string? AzureWebsiteSlotName { get; set; }

    public string? AzureWebsiteCloudRoleName { get; set; }
}

