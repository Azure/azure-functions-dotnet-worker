// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Target_GetCopyToOutputDirectoryItems_DoesNotIncludeFunctionsArtifacts()
    {
        // Arrange
        // Build a functions project so all artifacts are generated.
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        project.Build(restore: true).Should().BeSuccessful().And.HaveNoIssues();

        // Act
        // Run GetCopyToOutputDirectoryItems — this is what P2P references call
        // on this project to collect items for copying to the referencing project's output.
        TargetResult? result = project.RunTarget("GetCopyToOutputDirectoryItems");

        // Assert
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);

        // Verify no functions-specific artifacts leak through.
        result.Items.Should().NotContain(
            i => i.GetMetadata("TargetPath").Contains("worker.config.json"),
            "worker.config.json should not leak through P2P GetCopyToOutputDirectoryItems");
        result.Items.Should().NotContain(
            i => i.GetMetadata("TargetPath").Contains(Constants.ExtensionsOutputFolder),
            ".azurefunctions files should not leak through P2P GetCopyToOutputDirectoryItems");
        result.Items.Should().NotContain(
            i => i.GetMetadata("TargetPath").Contains("extensions.json"),
            "extensions.json should not leak through P2P GetCopyToOutputDirectoryItems");
    }
}
