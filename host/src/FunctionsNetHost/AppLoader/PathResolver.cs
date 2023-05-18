namespace FunctionsNetHost
{
    internal static partial class PathResolver
    {
        private static string? dotnetRootPath;
        private static string? hostFxrPath;

        internal static string GetDotnetRootPath()
        {
            if (dotnetRootPath == null)
            {
#if LINUX
            dotnetRootPath = GetUnixDotnetRootPath();
#else
                dotnetRootPath = GetWindowsDotnetRootPath();
#endif
            }

            return dotnetRootPath;
        }



        internal static string GetHostFxrPath()
        {
            if (hostFxrPath == null)
            {
#if LINUX
            hostFxrPath = GetUnixHostFxrPath();
#else
                hostFxrPath = GetWindowsHostFxrPath();
#endif
                if (!File.Exists(hostFxrPath))
                {
                    throw new FileNotFoundException(hostFxrPath);
                }
            }

            return hostFxrPath;
        }

        private static string GetLatestVersion(string hostFxrVersionsDirPath)
        {
            // Exclude the preview version for now. Will revisit.
            var versionDirectories = Directory.GetDirectories(hostFxrVersionsDirPath)
                .Where(f => !f.Contains("-preview")).ToArray();

            Array.Sort(versionDirectories);
            var latestVersion = Path.GetFileName(versionDirectories[^1]);

            return latestVersion;
        }
    }
}
