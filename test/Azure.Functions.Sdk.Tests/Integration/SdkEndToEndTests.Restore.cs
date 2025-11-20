// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection.Emit;
using System.Xml;
using Microsoft.Build.Utilities.ProjectCreation;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests : MSBuildSdkTestBase
{
    private string GeneratedProjectDirectory => TestRootPath + "/obj/azure_functions";
    private string GeneratedProjectPath => GeneratedProjectDirectory + "/azure_functions.g.csproj";
    private string GeneratedHashPath => GeneratedProjectDirectory + "/azure_functions.package.hash";
    private string GeneratedProjectLockFile => GeneratedProjectDirectory + "/obj/project.assets.json";

    [Fact]
    public void Restore_Success()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        ValidateProject([]);
    }

    [Fact]
    public void Restore_WithPackages_Success()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act
        BuildOutput output = project.Restore();

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        ValidateProject(
            [
                .. NugetPackage.ServiceBus.WebJobsPackages,
                .. NugetPackage.StorageBlobs.WebJobsPackages,
                .. NugetPackage.StorageQueues.WebJobsPackages,
            ]);
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

        File.Exists(GeneratedProjectPath).Should().BeFalse();
        File.Exists(GeneratedHashPath).Should().BeFalse();
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
                .. NugetPackage.ServiceBus.WebJobsPackages,
                .. NugetPackage.StorageBlobs.WebJobsPackages,
                .. NugetPackage.StorageQueues.WebJobsPackages,
            ]);

        FileInfo generated = new(GeneratedProjectPath);
        FileInfo hash = new(GeneratedHashPath);

        DateTime generatedWrite = generated.LastWriteTimeUtc;
        DateTime hashWrite = hash.LastWriteTimeUtc;

        // Act 2
        BuildOutput output2 = project.Restore();
        output2.Should().BeSuccessful().And.HaveNoIssues();
        generated.Refresh();
        hash.Refresh();

        generated.LastWriteTimeUtc.Should().Be(generatedWrite);
        hash.LastWriteTimeUtc.Should().Be(hashWrite);
    }

    private void ValidateProject(params NugetPackage[] packages)
    {
        ValidateProjectContents(packages);
        ValidateProjectRestored();
    }

    private void ValidateProjectContents(params NugetPackage[] packages)
    {
        FileInfo hashFile = new(GeneratedHashPath);
        hashFile.Exists.Should().BeTrue();

        FileInfo file = new(GeneratedProjectPath);
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

    private void ValidateProjectRestored()
    {
        Action read = () => LockFile.Read(GeneratedProjectLockFile);
        read.Should().NotThrow();
    }
}
