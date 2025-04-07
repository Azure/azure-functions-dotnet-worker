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
        public static readonly string PathToRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));
        public static readonly string TestRoot = Path.Combine(PathToRepoRoot, "test");
        public static readonly string SamplesRoot = Path.Combine(PathToRepoRoot, "samples");
        public static readonly string TestResourcesProjectsRoot = Path.Combine(TestRoot, "Resources", "Projects");

        public static void ValidateFunctionsMetadata(string actualFilePath, string embeddedResourceName)
        {
            JToken functionsMetadataContents = JToken.Parse(File.ReadAllText(actualFilePath));
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(embeddedResourceName));
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            JsonSerializer serializer = new JsonSerializer();
            var expected = serializer.Deserialize<JToken>(jsonReader);
            Assert.True(JToken.DeepEquals(functionsMetadataContents, expected), $"Actual: {functionsMetadataContents}{Environment.NewLine}Expected: {expected}");
        }

        public static async Task RemoveDockerTestImage(string repository, string imageTag, ITestOutputHelper outputHelper)
        {
            outputHelper.WriteLine($"Removing image {repository}:{imageTag} from local registry");
            int? rmiExitCode = await ProcessWrapper.RunProcessAsync("docker", $"rmi -f {repository}:{imageTag}", log: outputHelper.WriteLine);
            Assert.True(rmiExitCode.HasValue && rmiExitCode.Value == 0); // daemon may still error if the image doesn't exist, but it will still return 0
        }
    }
}
