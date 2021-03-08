// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Functions.Worker
{
    internal class WorkerInformation
    {
        private static Assembly _thisAssembly = typeof(WorkerInformation).Assembly;
        private static FileVersionInfo _fileVersionInfo = FileVersionInfo.GetVersionInfo(_thisAssembly.Location);

        private WorkerInformation()
        {
        }

        public static WorkerInformation Instance = new WorkerInformation();

        public int ProcessId { get; } = Process.GetCurrentProcess().Id;

        public string WorkerVersion { get; } = _thisAssembly.GetName().Version?.ToString()!;

        public string? ProductVersion { get; } = _fileVersionInfo.ProductVersion;

        public string FrameworkDescription => RuntimeInformation.FrameworkDescription;

        public string OSDescription => RuntimeInformation.OSDescription;

        public Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

        public string RuntimeIdentifier => RuntimeInformation.RuntimeIdentifier;

        public string CommandLine => Environment.CommandLine;
    }
}
