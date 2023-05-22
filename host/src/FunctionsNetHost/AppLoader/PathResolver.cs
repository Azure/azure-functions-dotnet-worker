// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    internal static partial class PathResolver
    {
        private static string? _hostFxrPath;

        internal static string GetHostFxrPath()
        {
            if (_hostFxrPath != null)
            {
                return _hostFxrPath;
            }
#if LINUX
            hostFxrPath = GetUnixHostFxrPath();
#else
            _hostFxrPath = FunctionsNetHost.PathResolver.GetWindowsHostFxrPath();
#endif
            if (!File.Exists(_hostFxrPath))
            {
                throw new FileNotFoundException(_hostFxrPath);
            }

            return _hostFxrPath;
        }

        /// <summary>
        /// The hostfxr root folder has multiple child directories, one for each .net SDK version installed.
        /// We will get the latest one.
        /// </summary>
        /// <param name="hostFxrVersionsDirPath"></param>
        /// <returns></returns>
        private static string GetLatestVersion(string hostFxrVersionsDirPath)
        {
            var versions = Directory.GetDirectories(hostFxrVersionsDirPath,"*", SearchOption.TopDirectoryOnly);
            if (!ShouldUseDotNetPreviewVersions())
            {
                versions = versions.Where(f => !f.Contains("-preview", StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            
            Array.Sort(versions);
            var latestVersion = Path.GetFileName(versions[^1]);

            return latestVersion;
        }

        private static bool ShouldUseDotNetPreviewVersions()
        {
            var value = EnvironmentUtils.GetValue(EnvironmentSettingNames.UsePreviewNetSdk);
            Logger.LogDebug($"{EnvironmentSettingNames.UsePreviewNetSdk} environment variable value:{value}");
            
            return !string.IsNullOrEmpty(value);
        }
    }
}
