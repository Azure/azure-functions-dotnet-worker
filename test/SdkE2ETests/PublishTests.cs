// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkE2ETests
{
    public class PublishTests(ITestOutputHelper testOutputHelper) : IDisposable
    {
        private readonly ProjectBuilder _builder = new(
            testOutputHelper,
            Path.Combine(TestUtility.SamplesRoot, "FunctionApp", "FunctionApp.csproj"));

        [Theory]
        [InlineData("", false)]
        [InlineData("-r win-x86", false)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=false", true)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=false -r win-x86", true)]
        public async Task Publish(string parameters, bool metadataGenerated)
        {
            await RunPublishTest(parameters, metadataGenerated);
        }

        [Fact]
        // This test requires the Docker daemon to be installed and running
        // It is excluded through the SdkE2ETests_default.runsettings file from normal tests
        // To run the test, use `dotnet test -s SdkE2ETests_dockertests.runsettings`
        [Trait("Requirement", "Docker")]
        public async Task Publish_Container()
        {
            var repository = nameof(SdkE2ETests).ToLower();
            var imageTag = nameof(Publish_Container);

            // setup test environment state in case there is leftover data from previous runs
            await TestUtility.RemoveDockerTestImage(repository, imageTag, testOutputHelper);

            try
            {
                // perform the publish
                await RunPublishTest($"-t:PublishContainer -p:ContainerRepository={repository} -p:ContainerImageTag={imageTag}", false);

                // validate the image base
                Tuple<int?, string> inspectResults = await ProcessWrapper.RunProcessForOutputAsync(
                    "docker",
                    $"inspect {repository}:{imageTag} --format \"{{{{ index .Config.Labels \\\"org.opencontainers.image.base.name\\\"}}}}\"",
                    _builder.OutputPath,
                    testOutputHelper.WriteLine);

                var inspectExitCode = inspectResults.Item1;
                var inspectOutput = inspectResults.Item2;
                Assert.True(inspectExitCode.HasValue && inspectExitCode.Value == 0);
                Assert.Matches("mcr\\.microsoft\\.com/azure-functions/dotnet-isolated:(\\d)+-dotnet-isolated(\\d+\\.\\d+)", inspectOutput);
            }
            finally
            {
                // clean up
                await TestUtility.RemoveDockerTestImage(repository, imageTag, testOutputHelper);
            }
        }

        private async Task RunPublishTest(string additionalParams, bool metadataGenerated)
        {
            await _builder.PublishAsync(additionalParams);

            // Make sure files are in /.azurefunctions
            string azureFunctionsDir = Path.Combine(_builder.OutputPath, ".azurefunctions");
            Assert.True(Directory.Exists(azureFunctionsDir));

            // Verify files are present
            string metadataLoaderPath = Path.Combine(azureFunctionsDir, "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll");
            string extensionsJsonPath = Path.Combine(_builder.OutputPath, "extensions.json");
            string functionsMetadataPath = Path.Combine(_builder.OutputPath, "functions.metadata");
            Assert.True(File.Exists(extensionsJsonPath));
            Assert.True(File.Exists(metadataLoaderPath));
            Assert.NotEqual(metadataGenerated, File.Exists(functionsMetadataPath)); 

            // Verify extensions.json
            JObject jObjects = JObject.Parse(File.ReadAllText(extensionsJsonPath));
            JObject extensionsJsonContents = jObjects;
            JToken expected = JObject.FromObject(new
            {
                extensions = new[]
                {
                    new Extension("AzureStorageBlobs",
                        "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageBlobsWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Blobs, Version=5.3.1.0, Culture=neutral, PublicKeyToken=92742159e12e44c8",
                        @"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Blobs.dll"),
                    new Extension("AzureStorageQueues",
                        "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageQueuesWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Queues, Version=5.3.1.0, Culture=neutral, PublicKeyToken=92742159e12e44c8",
                        @"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Queues.dll"),
                    new Extension("Startup",
                        "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c",
                        @"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll"),
                }
            });
            Assert.True(JToken.DeepEquals(extensionsJsonContents, expected), $"Actual: {extensionsJsonContents}{Environment.NewLine}Expected: {expected}");

            // Verify functions.metadata
            if (metadataGenerated)
            {
                TestUtility.ValidateFunctionsMetadata(functionsMetadataPath, "Microsoft.Azure.Functions.SdkE2ETests.Contents.functions.metadata");
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private class Extension
        {
            public Extension(string name, string typeName, string hintPath)
            {
                Name = name;
                TypeName = typeName;
                HintPath = hintPath;
            }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("typeName")]
            public string TypeName { get; set; }

            [JsonProperty("hintPath")]
            public string HintPath { get; set; }
        }
    }
}
