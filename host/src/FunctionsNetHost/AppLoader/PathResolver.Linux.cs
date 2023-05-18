namespace FunctionsNetHost
{

    internal static partial class PathResolver
    {
        private static string GetUnixDotnetRootPath()
        {
            return Path.Combine($"{Path.DirectorySeparatorChar}usr",
                                               "share",
                                               "dotnet");
        }

        ///Example path: usr/share/dotnet/host/fxr/7.0.5
        private static string GetUnixHostFxrPath()
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            string hostFxrVersionsDirPath = Path.Combine(GetUnixDotnetRootPath(),
                                               "host",
                                               "fxr");

            var latestVersion = GetLatestVersion(hostFxrVersionsDirPath);

            string hostfxrPath = Path.Combine(
                hostFxrVersionsDirPath,
                latestVersion,
                "libhostfxr.so");

            return Path.GetFullPath(hostfxrPath);
        }
    }
}
