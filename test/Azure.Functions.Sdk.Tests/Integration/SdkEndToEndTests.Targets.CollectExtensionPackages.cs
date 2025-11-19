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
            .ItemPackageReference("System.Text.Json", "8.0.6")
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
            .ItemPackageReference("Microsoft.Azure.Functions.Worker.Extensions.ServiceBus", "5.23.0")
            .ItemPackageReference("Microsoft.Azure.Functions.Worker.Extensions.Storage", "6.8.0")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("CollectExtensionPackages")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().HaveCount(3);

        ValidatePackage(
            result.Items[0],
            "Microsoft.Azure.WebJobs.Extensions.ServiceBus",
            "5.17.0",
            "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus");

        ValidatePackage(
            result.Items[1],
            "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs",
            "5.3.6",
            "Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs");

        ValidatePackage(
            result.Items[2],
            "Microsoft.Azure.WebJobs.Extensions.Storage.Queues",
            "5.3.6",
            "Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues");
    }

    private static void ValidatePackage(
        ITaskItem package, string expectedId, string expectedVersion, string expectedSourceId)
    {
        package.Should().HaveItemSpec(expectedId)
            .And.HaveMetadata("Version", expectedVersion)
            .And.HaveMetadata("SourcePackageId", expectedSourceId)
            .And.HaveMetadata("IsImplicitlyDefined", "true");
    }
}
