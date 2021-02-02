// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests
{
    public class ProcessWrapper : IDisposable
    {
        private bool _isDisposed = false;
        private readonly int _processTimeoutMs;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ProcessStartInfo _startInfo;

        private Process? _runningProcess;

        public ProcessWrapper(string fileName, string arguments, string workingDirectory, ITestOutputHelper testOutputHelper, int processTimeoutMs = 3 * 60 * 1000)
        {
            _processTimeoutMs = processTimeoutMs;

            _startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                FileName = fileName,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (!string.IsNullOrEmpty(arguments))
            {
                _startInfo.Arguments = arguments;
            }

            _testOutputHelper = testOutputHelper;
        }

        public async Task<int?> RunProcess()
        {
            SemaphoreSlim processExitSemaphore = new SemaphoreSlim(0, 1);

            _runningProcess = Process.Start(_startInfo);
            _runningProcess.EnableRaisingEvents = true;
            _runningProcess.BeginOutputReadLine();
            _runningProcess.BeginErrorReadLine();
            _runningProcess.ErrorDataReceived += (s, o) =>
            {
                if (o.Data != null)
                {
                    _testOutputHelper.WriteLine("Error: " + o.Data);
                }
            };

            _runningProcess.OutputDataReceived += (s, o) =>
            {
                if (o.Data != null)
                {
                    _testOutputHelper.WriteLine(o.Data);
                }
            };

            _runningProcess.Exited += (s, e) =>
            {
                processExitSemaphore.Release();
            };

            if (!await processExitSemaphore.WaitAsync(_processTimeoutMs))
            {
                _testOutputHelper?.WriteLine($"Process '{_runningProcess.Id}' did not exit in {_processTimeoutMs}ms.");
                Dispose();
            }

            return _runningProcess?.ExitCode;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _runningProcess?.Kill();
                _isDisposed = true;
            }
        }
    }
}
