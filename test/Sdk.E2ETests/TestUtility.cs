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

namespace Microsoft.Azure.Functions.Sdk.E2ETests
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
        public static readonly string PathToRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        public static readonly string SrcRoot = Path.Combine(PathToRepoRoot, "src");
        public static readonly string SdkSolutionRoot = Path.Combine(PathToRepoRoot, "sdk");
        public static readonly string SdkProjectRoot = Path.Combine(SdkSolutionRoot, "Sdk");
        public static readonly string TestRoot = Path.Combine(PathToRepoRoot, "test");
        public static readonly string SamplesRoot = Path.Combine(PathToRepoRoot, "samples");
        public static readonly string LocalPackages = Path.Combine(PathToRepoRoot, "local");
        public static readonly string TestOutputDir = Path.Combine(Path.GetTempPath(), "FunctionsWorkerSdk.E2ETests");
        public static readonly string TestResourcesProjectsRoot = Path.Combine(TestRoot, "Resources", "Projects");

        public static readonly string NuGetOrgPackages = "https://api.nuget.org/v3/index.json";
        public static readonly string NuGetPackageSource = LocalPackages;
        public static readonly string SdkVersion = "99.99.99-test";
        public static readonly string SdkBuildProj = Path.Combine(PathToRepoRoot, "build", "Sdk.slnf");

        private static bool _isInitialized = false;

        public static async Task<string> InitializeTestAsync(ITestOutputHelper testOutputHelper, string testName)
        {
            if (!_isInitialized)
            {
                testOutputHelper.WriteLine($"Packing {SdkBuildProj} with version {SdkVersion}");
                string arguments = $"pack {SdkBuildProj} -c {Configuration} -o {LocalPackages} -p:Version={SdkVersion}";

                int? exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, arguments, SrcRoot, testOutputHelper);
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
                .Single(str => str.EndsWith(embeddedResourceName));
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

        public static async Task RestoreAndBuildProjectAsync(string fullPathToProjFile, string outputDir, string additionalParams, ITestOutputHelper outputHelper)
        {
            // Name of the csproj
            string projectNameToTest = Path.GetFileName(fullPathToProjFile);
            string projectFileDirectory = Path.GetDirectoryName(fullPathToProjFile);

            // Restore
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Restoring...");
            string dotnetArgs = $"restore {projectNameToTest} -s {NuGetOrgPackages} -s {LocalPackages} -p:SdkVersion={SdkVersion}";
            int? exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectFileDirectory, testOutputHelper: outputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Done.");

            // Build
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Building...");
            dotnetArgs = $"build {projectNameToTest} --configuration {Configuration} -o {outputDir} -p:SdkVersion={SdkVersion} {additionalParams}";
            exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectFileDirectory, testOutputHelper: outputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Done.");
        }

        public static async Task RestoreAndPublishProjectAsync(string fullPathToProjFile, string outputDir, string additionalParams, ITestOutputHelper outputHelper)
        {
            // Name of the csproj
            string projectNameToTest = Path.GetFileName(fullPathToProjFile);
            string projectFileDirectory = Path.GetDirectoryName(fullPathToProjFile);

            // Restore
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Restoring...");
            string dotnetArgs = $"restore {projectNameToTest} -s {NuGetOrgPackages} -s {LocalPackages} -p:SdkVersion={SdkVersion}";
            int? exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectFileDirectory, testOutputHelper: outputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Done.");

            // Publish
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Publishing...");
            dotnetArgs = $"publish {projectNameToTest} --configuration {Configuration} -o {outputDir} -p:SdkVersion={SdkVersion} {additionalParams}";
            exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectFileDirectory, testOutputHelper: outputHelper);
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

        public static async Task RemoveDockerTestImage(string repository, string imageTag, ITestOutputHelper outputHelper)
        {
            outputHelper.WriteLine($"Removing image {repository}:{imageTag} from local registry");
            int? rmiExitCode = await new ProcessWrapper().RunProcess("docker", $"rmi -f {repository}:{imageTag}", TestOutputDir, outputHelper);
            Assert.True(rmiExitCode.HasValue && rmiExitCode.Value == 0); // daemon may still error if the image doesn't exist, but it will still return 0
        }
    }
}
