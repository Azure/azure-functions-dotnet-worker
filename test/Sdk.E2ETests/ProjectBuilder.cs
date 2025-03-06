// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Sdk.E2ETests
{
    public sealed class ProjectBuilder(ITestOutputHelper logger, string project) : IDisposable
    {
#if DEBUG
        public const string Configuration = "Debug";
#elif RELEASE
        public const string Configuration = "Release";
#endif
        public static readonly string LocalPackages = Path.Combine(TestUtility.PathToRepoRoot, "local");
        public static readonly string SrcRoot = Path.Combine(TestUtility.PathToRepoRoot, "src");
        public static readonly string SdkSolutionRoot = Path.Combine(TestUtility.PathToRepoRoot, "sdk");
        public static readonly string SdkProjectRoot = Path.Combine(SdkSolutionRoot, "Sdk");
        public static readonly string DotNetExecutable = "dotnet";
        public static readonly string SdkVersion = "99.99.99-test";
        public static readonly string SdkBuildProj = Path.Combine(TestUtility.PathToRepoRoot, "build", "Sdk.slnf");
        public static readonly string NuGetOrgPackages = "https://api.nuget.org/v3/index.json";

        private static Task _initialization;
        private static object _sync;

        private readonly TempDirectory _tempDirectory = new();

        public string OutputPath => _tempDirectory.Path;

        public async Task RestoreAsync()
        {
            await LazyInitializer.EnsureInitialized(ref _initialization, ref _sync, InitializeAsync);
            logger.WriteLine("Restoring...");
            string dotnetArgs = $"restore {project} -s {NuGetOrgPackages} -s {LocalPackages} -p:SdkVersion={SdkVersion}";
            Stopwatch stopwatch = Stopwatch.StartNew();
            int? exitCode = await ProcessWrapper.RunProcessAsync(DotNetExecutable, dotnetArgs, log: logger.WriteLine);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            logger.WriteLine($"Done. ({stopwatch.ElapsedMilliseconds} ms)");
        }

        public async Task BuildAsync(string additionalParams = null, bool restore = true)
        {
            await LazyInitializer.EnsureInitialized(ref _initialization, ref _sync, InitializeAsync);

            if (restore)
            {
                await RestoreAsync();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            logger.WriteLine("Building...");
            string dotnetArgs = $"build {project} --no-restore -c {Configuration} -o {OutputPath} -p:SdkVersion={SdkVersion} {additionalParams}";

            if (Debugger.IsAttached)
            {
                dotnetArgs += " -bl";
            }

            int? exitCode = await ProcessWrapper.RunProcessAsync(DotNetExecutable, dotnetArgs, log: logger.WriteLine);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            logger.WriteLine($"Done. ({stopwatch.ElapsedMilliseconds} ms)");
        }

        public async Task PublishAsync(string additionalParams = null, bool restore = true)
        {
            await LazyInitializer.EnsureInitialized(ref _initialization, ref _sync, InitializeAsync);

            if (restore)
            {
                await RestoreAsync();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            logger.WriteLine($"Publishing...");
            string dotnetArgs = $"publish {project} --no-restore -c {Configuration} -o {OutputPath} -p:SdkVersion={SdkVersion} {additionalParams}";
            int? exitCode = await ProcessWrapper.RunProcessAsync(DotNetExecutable, dotnetArgs, log: logger.WriteLine);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            logger.WriteLine($"Done. ({stopwatch.ElapsedMilliseconds} ms)");
        }

        private async Task InitializeAsync()
        {
            logger.WriteLine($"Packing {SdkBuildProj} with version {SdkVersion}");
            string arguments = $"pack {SdkBuildProj} -c {Configuration} -o {LocalPackages} -p:Version={SdkVersion}";

            int? exitCode = await ProcessWrapper.RunProcessAsync(DotNetExecutable, arguments, SrcRoot, logger.WriteLine);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
        }

        public void Dispose() => _tempDirectory.Dispose();
    }
}
