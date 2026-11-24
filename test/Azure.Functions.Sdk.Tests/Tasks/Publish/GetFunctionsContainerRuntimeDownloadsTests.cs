// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks.Publish.Tests;

public sealed class GetFunctionsContainerRuntimeDownloadsTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly Mock<IBuildEngine> _buildEngine = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void DownloadsBothFrameworksAndPreservesExistingPackageVersions()
    {
        GetFunctionsContainerRuntimeDownloads task = CreateTask();
        TaskItem existingCore = Download("Microsoft.NETCore.App.Runtime.linux-x64", "[11.0.1]");
        existingCore.SetMetadata("CustomMetadata", "preserved");
        task.ExistingPackageDownloads =
        [
            existingCore,
            Download("Microsoft.NETCore.App.Runtime.linux-x64", "[11.0.3]"),
        ];

        task.Execute().Should().BeTrue();

        ITaskItem core = task.PackageDownloads.Single(item => item.ItemSpec == "Microsoft.NETCore.App.Runtime.linux-x64");
        ITaskItem aspNet = task.PackageDownloads.Single(item => item.ItemSpec == "Microsoft.AspNetCore.App.Runtime.linux-x64");
        core.GetMetadata("Version").Should().Be("[11.0.1];[11.0.3];[11.0.5]");
        core.GetMetadata("CustomMetadata").Should().Be("preserved");
        aspNet.GetMetadata("Version").Should().Be("[11.0.4];[11.0.5]");
    }

    [Fact]
    public void HonorsPinnedRuntimePolicyFromTemplate()
    {
        GetFunctionsContainerRuntimeDownloads task = CreateTask();
        task.UserRuntimeConfig = Path.Combine(_temp.Path, "runtimeconfig.template.json");
        File.WriteAllText(task.UserRuntimeConfig, """
            {
              "rollForward": "Disable",
              "framework": { "name": "Microsoft.NETCore.App", "version": "11.0.2" }
            }
            """);

        task.Execute().Should().BeTrue();

        task.PackageDownloads.Single(item => item.ItemSpec == "Microsoft.NETCore.App.Runtime.linux-x64")
            .GetMetadata("Version").Should().Be("[11.0.2]");
        task.PackageDownloads.Single(item => item.ItemSpec == "Microsoft.AspNetCore.App.Runtime.linux-x64")
            .GetMetadata("Version").Should().Be("[11.0.0];[11.0.2]");
    }

    [Fact]
    public void FailsForInvalidRuntimeConfigTemplate()
    {
        GetFunctionsContainerRuntimeDownloads task = CreateTask();
        task.UserRuntimeConfig = Path.Combine(_temp.Path, "runtimeconfig.template.json");
        File.WriteAllText(task.UserRuntimeConfig, """{ "frameworks": {} }""");

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_InvalidRuntimeConfig, task.UserRuntimeConfig, "frameworks must be an array");
    }

    [Fact]
    public void IgnoresOtherFrameworksWhileRestoringRuntimePacks()
    {
        GetFunctionsContainerRuntimeDownloads task = CreateTask();
        task.UserRuntimeConfig = Path.Combine(_temp.Path, "runtimeconfig.template.json");
        File.WriteAllText(task.UserRuntimeConfig, """
            { "frameworks": [ { "name": "Custom.Framework", "version": "1.0.0" } ] }
            """);

        task.Execute().Should().BeTrue();

        task.PackageDownloads.Should().HaveCount(2);
    }

    [Fact]
    public void FailsWhenSdkDoesNotProvideRuntimeFramework()
    {
        GetFunctionsContainerRuntimeDownloads task = CreateTask();
        task.RuntimeFrameworks = [RuntimeFramework("Microsoft.NETCore.App", "11.0.0")];

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_RuntimeFrameworkNotResolved, "Microsoft.AspNetCore.App", "net11.0");
    }

    private GetFunctionsContainerRuntimeDownloads CreateTask() => new()
    {
        BuildEngine = _buildEngine.Object,
        TargetFramework = "net11.0",
        KnownFrameworkReferences =
        [
            KnownFramework("Microsoft.NETCore.App", "11.0.5"),
            KnownFramework("Microsoft.AspNetCore.App", "11.0.4"),
        ],
        RuntimeFrameworks =
        [
            RuntimeFramework("Microsoft.NETCore.App", "11.0.0"),
            RuntimeFramework("Microsoft.AspNetCore.App", "11.0.0"),
        ],
    };

    private static ITaskItem KnownFramework(string name, string latestVersion)
    {
        TaskItem item = new(name);
        item.SetMetadata("TargetFramework", "net11.0");
        item.SetMetadata("LatestRuntimeFrameworkVersion", latestVersion);
        return item;
    }

    private static ITaskItem RuntimeFramework(string name, string version)
    {
        TaskItem item = new(name);
        item.SetMetadata("FrameworkName", name);
        item.SetMetadata("Version", version);
        return item;
    }

    private static TaskItem Download(string name, string version)
    {
        TaskItem item = new(name);
        item.SetMetadata("Version", version);
        return item;
    }
}
