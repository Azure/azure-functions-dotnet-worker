// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkE2ETests
{
    public sealed class InnerBuildTests(ITestOutputHelper testOutputHelper) : IDisposable
    {
        private readonly ProjectBuilder _builder = new(
            testOutputHelper,
            Path.Combine(TestUtility.TestResourcesProjectsRoot, "FunctionApp01", "FunctionApp01.csproj"));

        [Theory]
        [InlineData("", false)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=true", false)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=true -p:FunctionsWriteMetadataJson=false", false)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=true -p:FunctionsWriteMetadataJson=true", true)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=false", true)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=false -p:FunctionsWriteMetadataJson=false", false)]
        [InlineData("-p:FunctionsEnableWorkerIndexing=false -p:FunctionsWriteMetadataJson=true", true)]
        public async Task Build_ScansReferences(string parameters, bool metadataGenerated)
        {
            await _builder.BuildAsync(parameters);

            // Verify extensions.json contents
            string extensionsJsonPath = Path.Combine(_builder.OutputPath, "extensions.json");
            Assert.True(File.Exists(extensionsJsonPath));

            JToken extensionsJsonContents = JObject.Parse(File.ReadAllText(extensionsJsonPath));
            JToken expectedExtensionsJson = JObject.Parse(@"{
  ""extensions"": [
    {
      ""name"": ""AzureStorageBlobs"",
      ""typeName"": ""Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageBlobsWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Blobs, Version=5.3.1.0, Culture=neutral, PublicKeyToken=92742159e12e44c8"",
      ""hintPath"": ""./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Blobs.dll""
    },
    {
      ""name"": ""AzureStorageQueues"",
      ""typeName"": ""Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageQueuesWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Queues, Version=5.1.3.0, Culture=neutral, PublicKeyToken=92742159e12e44c8"",
      ""hintPath"": ""./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Queues.dll""
    },
    {
      ""name"": ""Startup"",
      ""typeName"": ""Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c"",
      ""hintPath"": ""./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll""
    },
  ]
}");

            Assert.True(JToken.DeepEquals(expectedExtensionsJson, extensionsJsonContents));

            // Verify functions.metadata contents
            string functionsMetadataPath = Path.Combine(_builder.OutputPath, "functions.metadata");
            Assert.Equal(metadataGenerated, File.Exists(functionsMetadataPath));

            if (!metadataGenerated)
            {
                return;
            }

            JToken functionsMetadataContents = JArray.Parse(File.ReadAllText(functionsMetadataPath));
            JToken expectedFunctionsMetadata = JArray.Parse(@"[
  {
    ""name"": ""FunctionApp01_Hello"",
    ""scriptFile"": ""FunctionApp01.dll"",
    ""entryPoint"": ""FunctionApp01.HttpFunction.Hello"",
    ""language"": ""dotnet-isolated"",
    ""properties"": {
      ""IsCodeless"": false
    },
    ""bindings"": [
      {
        ""name"": ""req"",
        ""direction"": ""In"",
        ""type"": ""httpTrigger"",
        ""authLevel"": ""Anonymous"",
        ""methods"": [
          ""get""
        ],
        ""properties"": {}
      },
      {
        ""name"": ""$return"",
        ""type"": ""http"",
        ""direction"": ""Out""
      }
    ]
  },
  {
    ""name"": ""FunctionLib01_Hello"",
    ""scriptFile"": ""FunctionLib01.dll"",
    ""entryPoint"": ""FunctionLib01.HttpFunction.Hello"",
    ""language"": ""dotnet-isolated"",
    ""properties"": {
      ""IsCodeless"": false
    },
    ""bindings"": [
      {
        ""name"": ""req"",
        ""direction"": ""In"",
        ""type"": ""httpTrigger"",
        ""authLevel"": ""Anonymous"",
        ""methods"": [
          ""get""
        ],
        ""properties"": {}
      },
      {
        ""name"": ""$return"",
        ""type"": ""http"",
        ""direction"": ""Out""
      }
    ]
  }
]");

            Assert.True(JToken.DeepEquals(expectedFunctionsMetadata, functionsMetadataContents));
        }

        public void Dispose() => _builder.Dispose();
    }
}
