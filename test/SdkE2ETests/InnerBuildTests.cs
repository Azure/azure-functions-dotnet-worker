// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkE2ETests
{
    public class InnerBuildTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public InnerBuildTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Build_ScansReferences()
        {
            string outputDir = await TestUtility.InitializeTestAsync(_testOutputHelper, nameof(Build_ScansReferences));
            string projectFileDirectory = Path.Combine(TestUtility.TestResourcesProjectsRoot, "FunctionApp01", "FunctionApp01.csproj");

            await TestUtility.RestoreAndBuildProjectAsync(projectFileDirectory, outputDir, null, _testOutputHelper);

            // Verify extensions.json contents
            string extensionsJsonPath = Path.Combine(outputDir, "extensions.json");
            Assert.True(File.Exists(extensionsJsonPath));

            JToken extensionsJsonContents = JObject.Parse(File.ReadAllText(extensionsJsonPath));
            JToken expectedExtensionsJson = JObject.Parse(@"{
  ""extensions"": [
    {
      ""name"": ""SqlDurabilityProvider"",
      ""typeName"": ""DurableTask.SqlServer.AzureFunctions.SqlDurabilityProviderStartup, DurableTask.SqlServer.AzureFunctions, Version=1.4.0.0, Culture=neutral, PublicKeyToken=2ea3c3a96309d850"",
      ""hintPath"": ""./.azurefunctions/DurableTask.SqlServer.AzureFunctions.dll""
    },
    {
      ""name"": ""DurableTask"",
      ""typeName"": ""Microsoft.Azure.WebJobs.Extensions.DurableTask.DurableTaskWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.DurableTask, Version=2.0.0.0, Culture=neutral, PublicKeyToken=014045d636e89289"",
      ""hintPath"": ""./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.DurableTask.dll""
    },
    {
      ""name"": ""Startup"",
      ""typeName"": ""Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c"",
      ""hintPath"": ""./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll""
    }
  ]
}");

            Assert.True(JToken.DeepEquals(expectedExtensionsJson, extensionsJsonContents));

            // Verify functions.metadata contents
            string functionsMetadataPath = Path.Combine(outputDir, "functions.metadata");
            Assert.True(File.Exists(functionsMetadataPath));

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
    }
}
