// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkE2ETests
{
    public class ProcessWrapper
    {
        public async Task<int?> RunProcess(string fileName, string arguments, string workingDirectory, ITestOutputHelper testOutputHelper = null)
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
                    testOutputHelper.WriteLine($"[{DateTime.UtcNow:O}] Error: {o.Data}");
                }
            };

            testProcess.OutputDataReceived += (s, o) =>
            {
                if (o.Data != null)
                {
                    testOutputHelper.WriteLine($"[{DateTime.UtcNow:O}] {o.Data}");
                }
            };

            testProcess.Exited += (s, e) =>
            {
                processExitSemaphore.Release();
            };

            int wait = 3 * 60 * 1000;
            if (!await processExitSemaphore.WaitAsync(wait))
            {
                testOutputHelper?.WriteLine($"Process '{testProcess.Id}' did not exit in {wait}ms.");
                testProcess.Kill();
            }

            return testProcess?.ExitCode;
        }
    }
}
