// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests : MSBuildSdkTestBase
{
    [Fact]
    public void Target_GetFunctionsExtensionFiles_NoPackageRefs()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Build(restore: true).Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("GetFunctionsExtensionFiles")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().HaveCount(MinExpectedExtensionFiles.Length);
        foreach (string file in MinExpectedExtensionFiles)
        {
            string expectedPath = Path.Combine(Constants.ExtensionsOutputFolder, file);
            result.Items.Should().ContainSingle(
                i => i.GetMetadata("TargetPath").Equals(expectedPath));
        }
    }

    [Fact]
    public void Target_GetFunctionsExtensionFiles_WithPackageRefs()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .ItemPackageReference(NugetPackage.ServiceBus)
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Build(restore: true).Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("GetFunctionsExtensionFiles")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().NotBeEmpty();
        result.Items.Should().ContainSingle(i => i.ItemSpec.EndsWith("Microsoft.Azure.WebJobs.Extensions.ServiceBus.dll"));
        result.Items.Should().ContainSingle(i => i.ItemSpec.EndsWith("Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll"));
        result.Items.Should().AllSatisfy(i =>
        {
            if (i.ItemSpec.EndsWith("deps.json"))
            {
                i.Should().HaveMetadata(
                    "TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "function.deps.json"));
            }
            else
            {
                i.Should().HaveMetadataLike("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "*"));
            }
        });
    }
}
