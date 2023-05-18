namespace FunctionsNetHost
{

    internal partial class PathResolver
    {

        private static string GetWindowsDotnetRootPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet");
        }

        private static string GetWindowsHostFxrPath()
        {
            string hostFxrVersionsDirPath = Path.Combine(
                GetWindowsDotnetRootPath(),
                "host",
                "fxr");

            var latestVersion = GetLatestVersion(hostFxrVersionsDirPath);

            string hostfxrPath = Path.Combine(
                hostFxrVersionsDirPath,
                latestVersion,
                "hostfxr.dll");

            return Path.GetFullPath(hostfxrPath);
        }
    }
}
