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

        public static readonly string NuGetPackageSource = LocalPackages;

        public static async Task<string> InitializeTestAsync(ITestOutputHelper testOutputHelper, string testName)
        {
            // If there is already a local package, use it.
            testOutputHelper.WriteLine($"Looking for local nuget packages in '{LocalPackages}':");
            bool foundFiles = false;
            if (Directory.Exists(LocalPackages))
            {
                foreach (var file in Directory.GetFiles(LocalPackages, $"{WorkerSdkPackageName}*.nupkg"))
                {
                    testOutputHelper.WriteLine($"  {Path.GetFileName(file)}");
                    foundFiles = true;
                }
            }

            if (!foundFiles)
            {
                // Pack the Worker SDK into /local
                await PackWorkerSdk(testOutputHelper);
            }

            // Update the sample app to use the latest package from /local
            await UpdateNugetPackagesForApp(Path.Combine(SamplesRoot, "FunctionApp", "FunctionApp.csproj"), testOutputHelper);

            // Build .NET Worker
            string dotnetArgs = $"build --configuration {Configuration}";
            int? exitCode = await new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, Path.Combine(SrcRoot, "DotNetWorker"), testOutputHelper: testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);

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

        private static string InitializeOutputDir(string testName)
        {
            string outputDir = Path.Combine(TestOutputDir, testName);

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
                Directory.CreateDirectory(outputDir);
            }

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
