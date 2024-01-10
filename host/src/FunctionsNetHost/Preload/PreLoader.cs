using System.Diagnostics;

namespace FunctionsNetHost.PreLoad
{
    internal static class PreLoader
    {
        internal static void Load(string path)
        {
            try
            {
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
