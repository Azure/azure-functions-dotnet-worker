namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    /// <summary>
    /// Options for configuring Functions Application Insights.
    /// </summary>
    internal class FunctionsApplicationInsightsOptions
    {
        internal TokenCredentialOptions? TokenCredentialOptions { get; set; }
    }
}
