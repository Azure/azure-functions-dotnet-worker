// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests : MSBuildSdkTestBase
{
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
    }
}
