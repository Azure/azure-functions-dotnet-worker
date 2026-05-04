// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Property_Defaults_AreSet()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.TryGetPropertyValue("OutputType", out string outputType);
        project.TryGetPropertyValue("AzureFunctionsVersion", out string functionsVersion);

        // Assert
        outputType.Should().Be("Exe");
        functionsVersion.Should().Be("v4");
    }

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net7.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public void Property_ToolingSuffix_NetCore_MatchesTargetFramework(string tfm)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: tfm);

        // Act
        project.TryGetPropertyValue("FunctionsToolingSuffix", out string toolingSuffix);

        // Assert
        string expected = $"{tfm[..tfm.IndexOf('.')]}-isolated";
        toolingSuffix.Should().Be(expected);
    }

    [Theory]
    [InlineData("net48")]
    [InlineData("net481")]
    public void Property_ToolingSuffix_NetFx_MatchesTargetFramework(string tfm)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: tfm);

        // Act
        project.TryGetPropertyValue("FunctionsToolingSuffix", out string toolingSuffix);

        // Assert
        toolingSuffix.Should().Be("netfx-isolated");
    }

    [Fact]
    public void Property_AnalyzerProperties_ArePresent()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act & Assert
        foreach (string property in CompilerVisiblePropertiesExpected)
        {
            project.TryGetPropertyValue(property, out string? value);
            value.Should().NotBeNullOrEmpty($"Property '{property}' should have a value.");
        }
    }
}
