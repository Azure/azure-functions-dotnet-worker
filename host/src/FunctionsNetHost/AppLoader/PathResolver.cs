namespace FunctionsNetHost
{
    internal static partial class PathResolver
    {
        private static string? _dotnetRootPath;
        private static string? _hostFxrPath;

        internal static string GetDotnetRootPath()
        {
            if (_dotnetRootPath == null)
            {
#if LINUX
            dotnetRootPath = GetUnixDotnetRootPath();
#else
                _dotnetRootPath = GetWindowsDotnetRootPath();
#endif
            }

            return _dotnetRootPath;
        }



        internal static string GetHostFxrPath()
        {
            if (_hostFxrPath == null)
            {
#if LINUX
            hostFxrPath = GetUnixHostFxrPath();
#else
                _hostFxrPath = GetWindowsHostFxrPath();
#endif
                if (!File.Exists(_hostFxrPath))
                {
                    throw new FileNotFoundException(_hostFxrPath);
                }
            }

            return _hostFxrPath;
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
