using System.Diagnostics;

namespace FunctionsNetHost.PreLoad
{
    internal static class PreLoader
    {
        internal static void Load(string executableDir)
        {
            string platform = "windows";
#if OS_LINUX
            platform = "linux";
#endif
            string path = string.Empty;
            try
            {
                path = Path.Combine(executableDir, Path.Combine(Path.Combine(Path.Combine("PreloadAppsOut", platform), "net8.0"), "App.dll"));

                if (!File.Exists(path))
                {
                    Logger.Log($"File not found: {path}");
                    return;
                }

                if (!path.EndsWith(".dll"))
                {
                    Logger.Log($"File is not a dll: {path}");
                    return;
                }

                var url = "https://github.com/status";
                var process = new Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"{path} {url}";
                var started = process.Start();
                if (!started)
                {
                    Logger.Log($"Failed to start process: {path}");
                }
                else
                {
                    Logger.Log($"Started process: {path}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FunctionsNetHost.PreLoader. Failed to load: {path}.{ex}");
            }
        }
    }
}
