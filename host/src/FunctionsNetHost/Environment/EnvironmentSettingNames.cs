namespace FunctionsNetHost;

internal static class EnvironmentSettingNames
{
    /// <summary>
    /// Set value to "1" for enabling extra trace logs in FunctionsNetHost.
    /// </summary>
    internal const string FunctionsNetHostTrace = "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE";

    /// <summary>
    /// Set value to "1" to use preview version of hostfxr.
    /// </summary>
    internal const string UsePreviewNetSdk = "AZURE_FUNCTIONS_FUNCTIONSNETHOST_USE_PREVIEW_NETSDK";
}
