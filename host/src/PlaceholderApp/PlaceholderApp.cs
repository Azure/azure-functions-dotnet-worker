using System;
using System.IO;

using static System.Environment;

public class PlaceholderApp
{
    const string WorkerLogFileEnvVar = "AZURE_FUNCTIONS_WORKER_LOGFILE_PATH";

    static int Main(string[] args)
    {
        LogMessage($"Hello from placeholder app");

        return ExitCode;
    }

    private static void LogMessage(string message)
    {
        string logFile = GetEnvironmentVariable(WorkerLogFileEnvVar);

        if (string.IsNullOrWhiteSpace(logFile))
            Console.WriteLine($"PLACEHOLDER CONSOLE: {message}");
        else
            File.AppendAllTextAsync(logFile, $"{message}{NewLine}");
    }
}
