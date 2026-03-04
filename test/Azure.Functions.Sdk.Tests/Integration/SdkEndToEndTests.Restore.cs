// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Xml;
using Microsoft.Build.Utilities.ProjectCreation;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests : MSBuildSdkTestBase
{
    [Theory]
    [InlineData("net8.0")]
    [InlineData("net10.0")]
    [InlineData("net481")]
    public void Restore_Success(string tfm)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            path: GetTempCsproj(), targetFramework: tfm);

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        ValidateProject([]);
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net10.0")]
    [InlineData("net481")]
    [InlineData("net8.0;net10.0;net481")]
    public void Restore_MultiTarget_Success(string tfms)
    {
        // Arrange
        string[] targetFrameworks = tfms.Split(';');
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: null)
            .TargetFrameworks(targetFrameworks);

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        foreach (string tfm in targetFrameworks)
        {
            ValidateProject(tfm, []);
        }
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net10.0")]
    [InlineData("net481")]
    public void Restore_WithPackages_Success(string tfm)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            path: GetTempCsproj(), targetFramework: tfm)
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        ValidateProject(
            [
                ..NugetPackage.ServiceBus.WebJobsPackages,
                ..NugetPackage.StorageBlobs.WebJobsPackages,
                ..NugetPackage.StorageQueues.WebJobsPackages,
            ]);
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net10.0")]
    [InlineData("net481")]
    [InlineData("net8.0;net10.0;net481")]
    public void Restore_WithPackages_MultiTfm_Success(string tfms)
    {
        // Arrange
        string[] targetFrameworks = tfms.Split(';');
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            path: GetTempCsproj(), targetFramework: null)
            .TargetFrameworks(targetFrameworks)
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        foreach (string tfm in targetFrameworks)
        {
            ValidateProject(
                tfm,
                [
                    ..NugetPackage.ServiceBus.WebJobsPackages,
                    ..NugetPackage.StorageBlobs.WebJobsPackages,
                    ..NugetPackage.StorageQueues.WebJobsPackages,
                ]);
        }
    }

    [Fact]
    public void Restore_InvalidFunctionsVersion_Fail()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
           GetTempCsproj(),
            configure: p => p.Property("AzureFunctionsVersion", "v3"));

        // Act
        BuildOutput output = project.Restore();

        // Assert
        LogMessage logMessage = LogMessage.Error_UnknownFunctionsVersion;
        output.Should().BeFailed()
            .And.HaveNoWarnings()
            .And.HaveSingleError()
            .Which.Should().BeSdkMessage((logMessage, "v3"))
            .And.HaveSender("FuncSdkLog");

        GeneratedProject p = GeneratedProject.Create(TestRootPath, null);
        File.Exists(p.ProjectPath).Should().BeFalse();
        File.Exists(p.HashPath).Should().BeFalse();
    }

    [Fact]
    public void Restore_Incremental_NoOp()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act 1
        BuildOutput output = project.Restore();

        // Assert 2
        output.Should().BeSuccessful().And.HaveNoIssues();
        ValidateProject(
            [
                ..NugetPackage.ServiceBus.WebJobsPackages,
                ..NugetPackage.StorageBlobs.WebJobsPackages,
                ..NugetPackage.StorageQueues.WebJobsPackages,
            ]);

        GeneratedProject p = GeneratedProject.Create(TestRootPath, null);
        FileInfo generated = new(p.ProjectPath);
        FileInfo hash = new(p.HashPath);
        FileInfo marker = new(p.RestoreMarker);

        DateTime generatedWrite = generated.LastWriteTimeUtc;
        DateTime hashWrite = hash.LastWriteTimeUtc;
        DateTime markerWrite = marker.LastWriteTimeUtc;

        // Act 2
        BuildOutput output2 = project.Restore();
        output2.Should().BeSuccessful().And.HaveNoIssues();
        generated.Refresh();
        hash.Refresh();
        marker.Refresh();

        generated.LastWriteTimeUtc.Should().Be(generatedWrite);
        hash.LastWriteTimeUtc.Should().Be(hashWrite);
        marker.LastWriteTimeUtc.Should().BeAfter(markerWrite); // always updated.
    }

    [Theory]
    [InlineData("Microsoft.NET.Sdk.Functions")]
    [InlineData("Microsoft.Azure.Functions.Worker.Sdk")]
    public void Restore_IncompatibleSdk_Fails(string package)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .ItemPackageReference(package, "1.0.0");

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeFailed()
            .And.HaveNoWarnings()
            .And.HaveSingleError()
            .Which.Should().BeSdkMessage(
                (LogMessage.Error_UsingIncompatibleSdk, package))
            .And.HaveSender("FuncSdkLog");
    }

    private static void ValidateProjectContents(GeneratedProject project, params NugetPackage[] packages)
    {
        File.Exists(project.HashPath).Should().BeTrue();
        FileInfo file = new(project.ProjectPath);
        file.Exists.Should().BeTrue();

        using Stream stream = file.OpenRead();
        XmlDocument doc = new();
        Action act = () => doc.Load(stream);
        act.Should().NotThrow();

        doc.DocumentElement.Should().NotBeNull()
            .And.HaveAttribute("Sdk", ThisAssembly.Name);

        doc.SelectNodes("/Project/PropertyGroup")!.Count.Should().Be(0);

        int i = 0;
        XmlNodeList nodes = doc.SelectNodes("/Project/ItemGroup/PackageReference")!;
        nodes.Count.Should().Be(packages.Length);
        foreach (XmlElement node in nodes)
        {
            node.Should().HaveAttribute("Include", packages[i].Name);
            node.Should().HaveAttribute("Version", packages[i].Version);
            i++;
        }
    }

    private static void ValidateProjectRestored(GeneratedProject project)
    {
        Action read = () => LockFile.Read(project.AssetsPath);
        read.Should().NotThrow();

        File.Exists(project.RestoreMarker).Should().BeTrue();
    }

    private void ValidateProject(params NugetPackage[] packages)
    {
        ValidateProject(tfm: null, packages);
    }

    private void ValidateProject(string? tfm, params NugetPackage[] packages)
    {
        GeneratedProject project = GeneratedProject.Create(TestRootPath, tfm);
        ValidateProjectContents(project, packages);
        ValidateProjectRestored(project);
    }

    private record GeneratedProject(string ProjectPath, string HashPath, string AssetsPath, string RestoreMarker)
    { 
        public static GeneratedProject Create(string rootPath, string? tfm)
        {
            string directory = string.IsNullOrEmpty(tfm)
                ? $"{rootPath}/obj/azure_functions"
                : $"{rootPath}/obj/azure_functions_{tfm}";
            return new(
                ProjectPath: $"{directory}/azure_functions.g.csproj",
                HashPath: $"{directory}/azure_functions.g.csproj.hash",
                AssetsPath: $"{directory}/obj/project.assets.json",
                RestoreMarker: $"{directory}/restored.marker");
        }
    }
}
