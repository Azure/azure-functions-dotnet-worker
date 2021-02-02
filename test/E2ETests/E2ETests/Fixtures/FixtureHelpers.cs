// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Tests.E2ETests

{
    public static class FixtureHelpers
    {
        public static Process GetFuncHostProcess(bool enableAuth = false)
        {
            var funcProcess = new Process();
            var rootDir = Path.GetFullPath(@"..\..\..\..\..\..");

            funcProcess.StartInfo.UseShellExecute = false;
            funcProcess.StartInfo.RedirectStandardError = true;
            funcProcess.StartInfo.RedirectStandardOutput = true;
            funcProcess.StartInfo.CreateNoWindow = true;
            funcProcess.StartInfo.WorkingDirectory = Path.Combine(rootDir, @"Test\E2ETests\E2EApps\CosmosApp\bin\Debug\net5.0");
            funcProcess.StartInfo.FileName = Path.Combine(rootDir, @"Azure.Functions.Cli\func.exe");
            funcProcess.StartInfo.ArgumentList.Add("host");
            funcProcess.StartInfo.ArgumentList.Add("start");
            funcProcess.StartInfo.ArgumentList.Add("--csharp");
            if (enableAuth)
            {
                funcProcess.StartInfo.ArgumentList.Add("--enableAuth");
            }

            return funcProcess;
        }

        public static void StartProcessWithLogging(Process funcProcess, ILogger logger)
        {
            funcProcess.ErrorDataReceived += (sender, e) => logger.LogError(e?.Data);
            funcProcess.OutputDataReceived += (sender, e) => logger.LogInformation(e?.Data);

            funcProcess.Start();

            funcProcess.BeginErrorReadLine();
            funcProcess.BeginOutputReadLine();
        }

        public static void KillExistingFuncHosts()
        {
            foreach (var func in Process.GetProcessesByName("func"))
            {
                func.Kill();
            }
        }
    }
}
