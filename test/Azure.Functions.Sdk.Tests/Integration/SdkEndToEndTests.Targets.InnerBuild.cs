// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Theory]
    [InlineData("8.0", "net8.0")]
    [InlineData("9.0", "net9.0")]
    [InlineData("10.0", "net10.0")]
    [InlineData("11.0", "net10.0")]
    public void Target_InnerBuild_TargetFramework_MatchesSdkWithHostCap(string sdkMaximumVersion, string expectedTargetFramework)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Create(
            path: Path.Combine(TestRootPath, "azure_functions.g.csproj"),
            sdk: "Azure.Functions.Sdk/99.99.99",
            globalProperties: new Dictionary<string, string>
            {
                ["NETCoreAppMaximumVersion"] = sdkMaximumVersion,
            });

        // Act
        project.TryGetPropertyValue("TargetFramework", out string targetFramework);
        project.TryBuild("_VerifyTargetFramework", null, out _, out BuildOutput output, out _);

        // Assert
        targetFramework.Should().Be(expectedTargetFramework);
        output.Should().BeSuccessful().And.HaveNoIssues();
    }

    [Theory]
    [InlineData("8.0", "net8.0", "net481", "Restore")]
    [InlineData("9.0", "net9.0", "net8.0", "Restore")]
    [InlineData("10.0", "net10.0", "net8.0", "Restore")]
    [InlineData("10.0", "net10.0", "net9.0", "Restore")]
    [InlineData("10.0", "net10.0", "net481", "Restore")]
    [InlineData("11.0", "net10.0", "net11.0", "Restore")]
    [InlineData("8.0", "net8.0", "net481", "BeforeBuild")]
    [InlineData("9.0", "net9.0", "net8.0", "BeforeBuild")]
    [InlineData("10.0", "net10.0", "net8.0", "BeforeBuild")]
    [InlineData("10.0", "net10.0", "net9.0", "BeforeBuild")]
    [InlineData("10.0", "net10.0", "net481", "BeforeBuild")]
    [InlineData("11.0", "net10.0", "net11.0", "BeforeBuild")]
    public void Target_InnerBuild_TargetFrameworkOverride_Fails(
        string sdkMaximumVersion, string expectedTargetFramework, string targetFramework, string target)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Create(
            path: Path.Combine(TestRootPath, "azure_functions.g.csproj"),
            sdk: "Azure.Functions.Sdk/99.99.99",
            globalProperties: new Dictionary<string, string>
            {
                ["NETCoreAppMaximumVersion"] = sdkMaximumVersion,
                ["TargetFramework"] = targetFramework,
            });
        project.RootElement.Save();

        // Act
        project.TryBuild(target, null, out _, out BuildOutput output, out _);

        // Assert
        output.Should().BeFailed().And.HaveSingleError()
            .Which.Message.Should().Be(
                $"The target framework '{targetFramework}' must be '{expectedTargetFramework}'. Verify if target framework has been overridden by a global property.");
    }

    [Fact]
    public void Target_InnerBuild_Warning()
    {
        // Arrange
        // Create a normal Azure Functions project that will generate the inner azure_functions.g.csproj.
        // A custom target adds the generated project as a ProjectReference, simulating a traversal
        // project that accidentally globs it in. Building the outer project should fail because
        // the inner project's _BlockBuild target fires.
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net$(NETCoreAppMaximumVersion)")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs)
            .ItemProjectReference("**/azure_functions.g.csproj");

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        BuildOutput output = project.Build();

        // Assert
        output.Should().BeSuccessful()
            .And.HaveSingleWarning()
            .Which.Should().BeSdkMessage(LogMessage.Warning_GeneratedProjectShouldNotBeBuilt)
            .And.HaveSender("FuncSdkLog");
    }
}
