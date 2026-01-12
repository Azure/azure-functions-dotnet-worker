// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Tests.E2ETests

{
    public static class FixtureHelpers
    {
        public static Process GetFuncHostProcess(bool enableAuth = false, string testAppName = null)
        {
            Process funcProcess = new();
            string? e2eAppPath = Path.Combine(TestUtility.RepoRoot, "test", "E2ETests", "E2EApps", testAppName);

            funcProcess.StartInfo.UseShellExecute = false;
            funcProcess.StartInfo.RedirectStandardError = true;
            funcProcess.StartInfo.RedirectStandardOutput = true;
            funcProcess.StartInfo.CreateNoWindow = true;
            funcProcess.StartInfo.WorkingDirectory = e2eAppPath;
            funcProcess.StartInfo.FileName = "dotnet";
            funcProcess.StartInfo.ArgumentList.Add("run");
            funcProcess.StartInfo.ArgumentList.Add("--no-build");

            if (enableAuth)
            {
                // '--' to pass args to func host
                funcProcess.StartInfo.ArgumentList.Add("--");
                funcProcess.StartInfo.ArgumentList.Add("--enableAuth");
            }

            return funcProcess;
        }

        public static void StartProcessWithLogging(Process funcProcess, ILogger logger)
        {
            funcProcess.ErrorDataReceived += (sender, e) => logger.LogError(e?.Data);
            funcProcess.OutputDataReceived += (sender, e) => logger.LogInformation(e?.Data);

            funcProcess.Start();

            logger.LogInformation($"Started '{funcProcess.StartInfo.FileName}'");

            funcProcess.BeginErrorReadLine();
            funcProcess.BeginOutputReadLine();
        }

        public static void KillExistingFuncHosts()
        {
            foreach (var func in Process.GetProcessesByName("func"))
            {
                try
                {
                    func.Kill();
                }
                catch
                {
                    // Best effort
                }
            }
        }
    }
}
