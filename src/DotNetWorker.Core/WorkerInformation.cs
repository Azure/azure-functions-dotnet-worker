// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

// This class is instantiated and used in serialization.
#pragma warning disable CA1822

namespace Microsoft.Azure.Functions.Worker
{
    internal sealed class WorkerInformation
    {
        private static readonly Assembly _thisAssembly = typeof(WorkerInformation).Assembly;
        private static readonly FileVersionInfo _fileVersionInfo = FileVersionInfo.GetVersionInfo(_thisAssembly.Location);

        private string? _workerVersion;

        public static WorkerInformation Instance = new();

#if NET5_0_OR_GREATER
        public int ProcessId => Environment.ProcessId;

        public string RuntimeIdentifier => RuntimeInformation.RuntimeIdentifier;
#else
        public int ProcessId => Process.GetCurrentProcess().Id;

        public string RuntimeIdentifier => "n/a"; // Resolve in netstandard
#endif

        public string WorkerVersion => _workerVersion ??= _thisAssembly.GetName().Version?.ToString()!;


        public string? ProductVersion => _fileVersionInfo.ProductVersion;

        public string FrameworkDescription => RuntimeInformation.FrameworkDescription;

        public string OSDescription => RuntimeInformation.OSDescription;

        public Architecture OSArchitecture => RuntimeInformation.OSArchitecture;


        public string CommandLine => Environment.CommandLine;
    }
}
#pragma warning restore CA1822
