// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkE2ETests
{
    public static class TestUtility
    {
        // Sdk package name
        public const string WorkerSdkPackageName = "Microsoft.Azure.Functions.Worker.Sdk";

        // Configurations
        public const string Configuration = "Debug";
        public const string NetCoreFramework = "netcoreapp3.1";
        public const string NetFramework = "net461";
        public const string NetStandard = "netstandard2.0";
        public const string Net50 = "net5.0";

        // Paths and executables
        public static readonly string DotNetExecutable = "dotnet";

        public static readonly string PathToRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));
        public static readonly string SrcRoot = Path.Combine(PathToRepoRoot, "src");
        public static readonly string SdkSolutionRoot = Path.Combine(PathToRepoRoot, "sdk");
        public static readonly string SdkProjectRoot = Path.Combine(SdkSolutionRoot, "Sdk");
        public static readonly string TestRoot = Path.Combine(PathToRepoRoot, "test");
        public static readonly string SamplesRoot = Path.Combine(PathToRepoRoot, "samples");
        public static readonly string LocalPackages = Path.Combine(PathToRepoRoot, "local");
        public static readonly string TestOutputDir = Path.Combine(Path.GetTempPath(), "FunctionsWorkerSdkE2ETests");
        public static readonly string DevPackPath = Path.Combine(PathToRepoRoot, "tools", "devpack.ps1");

        public static readonly string NuGetPackageSource = LocalPackages;

        private static bool _isInitialized = false;

        public static async Task<string> InitializeTestAsync(ITestOutputHelper testOutputHelper, string testName)
        {
            if (!_isInitialized)
            {
                testOutputHelper.WriteLine($"Running {DevPackPath}");

                int? exitCode = await new ProcessWrapper().RunProcess("powershell", DevPackPath, SrcRoot, testOutputHelper);
                Assert.True(exitCode.HasValue && exitCode.Value == 0);

                // Build .NET Worker
                string dotnetArgs = $"build --configuration {Configuration}";
                exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, Path.Combine(SrcRoot, "DotNetWorker"), testOutputHelper: testOutputHelper);
                Assert.True(exitCode.HasValue && exitCode.Value == 0);

                _isInitialized = true;
            }

            return InitializeOutputDir(testName);
        }

        public static void ValidateFunctionsMetadata(string actualFilePath, string embeddedResourceName)
        {
            JToken functionsMetadataContents = JToken.Parse(File.ReadAllText(actualFilePath));
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith("functions.metadata"));
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        var expected = serializer.Deserialize<JToken>(jsonReader);
                        Assert.True(JToken.DeepEquals(functionsMetadataContents, expected), $"Actual: {functionsMetadataContents}{Environment.NewLine}Expected: {expected}");
                    }
                }
            }
        }

        public static async Task RestoreAndPublishProjectAsync(string fullPathToProjFile, string outputDir, string additionalParams, ITestOutputHelper outputHelper)
        {
            // Name of the csproj
            string projectNameToTest = Path.GetFileName(fullPathToProjFile);
            string projectFileDirectory = Path.GetDirectoryName(fullPathToProjFile);

            // Restore
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Restoring...");
            string dotnetArgs = $"restore {projectNameToTest} --source {TestUtility.LocalPackages}";
            int? exitCode = await new ProcessWrapper().RunProcess(TestUtility.DotNetExecutable, dotnetArgs, projectFileDirectory, testOutputHelper: outputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Done.");

            // Publish
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Publishing...");
            dotnetArgs = $"publish {projectNameToTest} --configuration {TestUtility.Configuration} -o {outputDir} {additionalParams}";
            exitCode = await new ProcessWrapper().RunProcess(TestUtility.DotNetExecutable, dotnetArgs, projectFileDirectory, testOutputHelper: outputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Done.");
        }

        private static string InitializeOutputDir(string testName)
        {
            string outputDir = Path.Combine(TestOutputDir, testName);

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }

            Directory.CreateDirectory(outputDir);

            return outputDir;
        }

        private static async Task PackWorkerSdk(ITestOutputHelper testOutputHelper)
        {
            // Build solution
            int? exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, $"build --configuration {Configuration}", SdkProjectRoot, testOutputHelper: testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);

            // Pack Sdk project
            exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, $"pack --configuration {Configuration} -o {LocalPackages} --no-build", SdkProjectRoot, testOutputHelper: testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
        }

        private static async Task UpdateNugetPackagesForApp(string projectFile, ITestOutputHelper testOutputHelper)
        {
            int? exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, $"remove {projectFile} package {WorkerSdkPackageName}", PathToRepoRoot, testOutputHelper: testOutputHelper);
            // If a previous run failed, this may have a -1 exit code. We'll continue anyway.

            exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, $"add {projectFile} package {WorkerSdkPackageName} -s {LocalPackages} --prerelease", SdkProjectRoot, testOutputHelper: testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
        }
    }
}
