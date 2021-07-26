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
    public class PublishTests
    {
        private ITestOutputHelper _testOutputHelper;

        public PublishTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Publish()
        {
            string outputDir = await TestUtility.InitializeTestAsync(_testOutputHelper, nameof(Publish));
            await RunPublishTest(outputDir);
        }

        [Fact]
        public async Task Publish_Rid()
        {
            string outputDir = await TestUtility.InitializeTestAsync(_testOutputHelper, nameof(Publish_Rid));
            await RunPublishTest(outputDir, "-r win-x86");
        }

        private async Task RunPublishTest(string outputDir, string additionalParams = null)
        {
            // Name of the csproj            
            string projectFileDirectory = Path.Combine(TestUtility.SamplesRoot, "FunctionApp", "FunctionApp.csproj");

            await TestUtility.RestoreAndPublishProjectAsync(projectFileDirectory, outputDir, additionalParams, _testOutputHelper);

            // Make sure files are in /.azurefunctions
            string azureFunctionsDir = Path.Combine(outputDir, ".azurefunctions");
            Assert.True(Directory.Exists(azureFunctionsDir));
            var files = Directory.EnumerateFiles(azureFunctionsDir);

            // Verify files are present
            string metadataLoaderPath = Path.Combine(azureFunctionsDir, "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll");
            string extensionsJsonPath = Path.Combine(outputDir, "extensions.json");
            string functionsMetadataPath = Path.Combine(outputDir, "functions.metadata");
            Assert.True(File.Exists(metadataLoaderPath));
            Assert.True(File.Exists(extensionsJsonPath));
            Assert.True(File.Exists(functionsMetadataPath));

            // Verify extensions.json
            JObject jObjects = JObject.Parse(File.ReadAllText(extensionsJsonPath));
            JObject extensionsJsonContents = jObjects;
            JToken expected = JObject.FromObject(new
            {
                extensions = new[]
                {
                    new Extension("Startup",
                        "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c",
                        @"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll"),
                    new Extension("AzureStorageBlobs",
                        "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageBlobsWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Blobs, Version=5.0.0.0, Culture=neutral, PublicKeyToken=92742159e12e44c8",
                        @"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Blobs.dll"),
                    new Extension("AzureStorageQueues",
                        "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageQueuesWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Queues, Version=5.0.0.0, Culture=neutral, PublicKeyToken=92742159e12e44c8",
                        @"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Queues.dll")
                }
            });
            Assert.True(JToken.DeepEquals(extensionsJsonContents, expected), $"Actual: {extensionsJsonContents}{Environment.NewLine}Expected: {expected}");

            // Verify functions.metadata
            TestUtility.ValidateFunctionsMetadata(functionsMetadataPath, "functions.metadata");
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
