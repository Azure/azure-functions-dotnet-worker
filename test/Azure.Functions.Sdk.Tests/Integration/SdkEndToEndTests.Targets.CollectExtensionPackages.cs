// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Target_CollectExtensionPackages_NoPackageRefs()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("CollectExtensionPackages")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Target_CollectExtensionPackages_NonExtensionPackage()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .ItemPackageReference(NugetPackage.SystemTextJson)
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("CollectExtensionPackages")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Target_CollectExtensionPackages()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage)
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("CollectExtensionPackages")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().HaveCount(3);

        ValidatePackage(result.Items[0], NugetPackage.ServiceBus);
        ValidatePackage(result.Items[1], NugetPackage.StorageBlobs);
        ValidatePackage(result.Items[2], NugetPackage.StorageQueues);
    }

    private static void ValidatePackage(ITaskItem package, WorkerPackage worker)
    {
        NugetPackage webJobs = worker.WebJobsPackages.Should().ContainSingle().Which;
        package.Should().HaveItemSpec(webJobs.Name)
            .And.HaveMetadata("Version", webJobs.Version)
            .And.HaveMetadata("SourcePackageId", worker.Name)
            .And.HaveMetadata("IsImplicitlyDefined", "true");
    }
}
