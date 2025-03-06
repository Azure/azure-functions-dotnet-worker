// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.SdkE2ETests
{
    public static class ProcessWrapper
    {
        public static async Task<int?> RunProcessAsync(
            string fileName, string arguments, string workingDirectory = null, Action<string> log = null)
        {
            return await RunProcessInternalAsync(fileName, arguments, workingDirectory, log);
        }

        public static async Task<Tuple<int?, string>> RunProcessForOutputAsync(
            string fileName, string arguments, string workingDirectory = null, Action<string> log = null)
        {
            StringBuilder processOutputStringBuilder = new StringBuilder();
            var exitCode = await RunProcessInternalAsync(fileName, arguments, workingDirectory, log, processOutputStringBuilder);
            return new Tuple<int?,string>(exitCode, processOutputStringBuilder.ToString());
        }

        private static async Task<int?> RunProcessInternalAsync(
            string fileName, string arguments, string workingDirectory = null, Action<string> log = null, StringBuilder processOutputBuilder = null)
        {

            SemaphoreSlim processExitSemaphore = new SemaphoreSlim(0, 1);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                FileName = fileName,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (!string.IsNullOrEmpty(arguments))
            {
                startInfo.Arguments = arguments;
            }

            Process testProcess = Process.Start(startInfo);
            testProcess.EnableRaisingEvents = true;
            testProcess.BeginOutputReadLine();
            testProcess.BeginErrorReadLine();
            testProcess.ErrorDataReceived += (s, o) =>
            {
                if (o.Data != null)
                {
                    log?.Invoke($"[{DateTime.UtcNow:O}] Error: {o.Data}");
                    processOutputBuilder?.AppendLine(o.Data);
                }
            };

            testProcess.OutputDataReceived += (s, o) =>
            {
                if (o.Data != null)
                {
                    log?.Invoke($"[{DateTime.UtcNow:O}] {o.Data}");
                    processOutputBuilder?.AppendLine(o.Data);
                }
            };

            testProcess.Exited += (s, e) =>
            {
                processExitSemaphore.Release();
            };

            int wait = 3 * 60 * 1000;
            if (!await processExitSemaphore.WaitAsync(wait))
            {
               log?.Invoke($"Process '{testProcess.Id}' did not exit in {wait}ms.");
                testProcess.Kill();
            }

            return testProcess?.ExitCode;
        }
    }

}
