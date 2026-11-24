// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks.Publish.Tests;

public sealed class ResolveFunctionsContainerRuntimePacksTests : IDisposable
{
    private const string CorePackage = "Microsoft.NETCore.App.Runtime.linux-x64";
    private const string AspNetPackage = "Microsoft.AspNetCore.App.Runtime.linux-x64";
    private readonly TempDirectory _temp = new();
    private readonly Mock<IBuildEngine> _buildEngine = new();

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("net11.0")]
    [InlineData("net11.0-windows")]
    public void UsesPackageFoldersFromRestoredAssets(string targetFramework)
    {
        ResolveFunctionsContainerRuntimePacks task = CreateTask(targetFramework);
        string corePath = CreatePack("primary", CorePackage, "11.0.0");
        string aspNetPath = CreatePack("fallback", AspNetPackage, "11.0.0");

        bool success = task.Execute();
        string errors = string.Join("; ", _buildEngine.Invocations
            .Where(invocation => invocation.Method.Name == nameof(IBuildEngine.LogErrorEvent))
            .Select(invocation => ((BuildErrorEventArgs)invocation.Arguments[0]).Message));
        success.Should().BeTrue(errors);

        task.CorePackPath.Should().Be(Path.Combine(corePath, "runtimes", "linux-x64"));
        task.AspNetPackPath.Should().Be(Path.Combine(aspNetPath, "runtimes", "linux-x64"));
    }

    [Fact]
    public void RejectsUnrestoredVersionEvenWhenPackageIsInCache()
    {
        ResolveFunctionsContainerRuntimePacks task = CreateTask();
        task.CoreRuntimeVersion = "11.0.1";
        CreatePack("primary", CorePackage, "11.0.1");
        CreatePack("fallback", AspNetPackage, "11.0.0");

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_RuntimeDownloadsMissing, task.TargetFramework, task.ProjectAssetsFile);
    }

    [Fact]
    public void ReportsMissingRestoredPackage()
    {
        ResolveFunctionsContainerRuntimePacks task = CreateTask();
        CreatePack("primary", CorePackage, "11.0.0");

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_RuntimePackNotFound, AspNetPackage, "11.0.0");
    }

    [Fact]
    public void ReportsIncompleteRuntimePack()
    {
        ResolveFunctionsContainerRuntimePacks task = CreateTask();
        string corePath = CreatePack("primary", CorePackage, "11.0.0");
        string aspNetPath = CreatePack("fallback", AspNetPackage, "11.0.0");
        File.Delete(Path.Combine(aspNetPath, "runtimes", "linux-x64", "lib", "net11.0", "Microsoft.AspNetCore.App.deps.json"));

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_RuntimePackIncomplete,
            Path.Combine(corePath, "runtimes", "linux-x64"), Path.Combine(aspNetPath, "runtimes", "linux-x64"));
    }

    private ResolveFunctionsContainerRuntimePacks CreateTask(string targetFramework = "net11.0")
    {
        string assetsPath = Path.Combine(_temp.Path, "project.assets.json");
        Directory.CreateDirectory(Path.Combine(_temp.Path, "fallback"));
        string primary = JsonSerializer.Serialize(Path.Combine(_temp.Path, "primary") + Path.DirectorySeparatorChar);
        string fallback = JsonSerializer.Serialize(Path.Combine(_temp.Path, "fallback") + Path.DirectorySeparatorChar);
        string projectPath = JsonSerializer.Serialize(Path.Combine(_temp.Path, "App.csproj"));
        string outputPath = JsonSerializer.Serialize(_temp.Path + Path.DirectorySeparatorChar);
        string tfm = JsonSerializer.Serialize(targetFramework);
        File.WriteAllText(assetsPath, $$"""
            {
              "version": 3,
              "targets": { {{tfm}}: {} },
              "libraries": {},
              "packageFolders": { {{primary}}: {}, {{fallback}}: {} },
              "project": {
                "version": "1.0.0",
                "restore": {
                  "projectUniqueName": {{projectPath}},
                  "projectName": "App",
                  "projectPath": {{projectPath}},
                  "packagesPath": {{primary}},
                  "outputPath": {{outputPath}},
                  "projectStyle": "PackageReference",
                  "originalTargetFrameworks": [ {{tfm}} ],
                  "frameworks": { {{tfm}}: { "targetAlias": {{tfm}}, "projectReferences": {} } }
                },
                "frameworks": {
                  {{tfm}}: {
                    "targetAlias": {{tfm}},
                    "downloadDependencies": [
                      { "name": "{{CorePackage}}", "version": "[11.0.0, 11.0.0]" },
                      { "name": "{{AspNetPackage}}", "version": "[11.0.0, 11.0.0]" }
                    ]
                  }
                }
              }
            }
            """);

        return new ResolveFunctionsContainerRuntimePacks
        {
            BuildEngine = _buildEngine.Object,
            ProjectAssetsFile = assetsPath,
            TargetFramework = targetFramework,
            RuntimeTargetFramework = "net11.0",
            CoreRuntimeVersion = "11.0.0",
            AspNetRuntimeVersion = "11.0.0",
        };
    }

    private string CreatePack(string folder, string packageId, string version)
    {
        string packPath = Path.Combine(_temp.Path, folder, packageId.ToLowerInvariant(), version);
        string runtimePath = Path.Combine(packPath, "runtimes", "linux-x64");
        string libPath = Path.Combine(runtimePath, "lib", "net11.0");
        Directory.CreateDirectory(libPath);
        File.WriteAllText(Path.Combine(packPath, $"{packageId.ToLowerInvariant()}.{version}.nupkg.sha512"), "hash");
        if (packageId == CorePackage)
        {
            string nativePath = Path.Combine(runtimePath, "native");
            Directory.CreateDirectory(nativePath);
            File.WriteAllText(Path.Combine(nativePath, "libhostfxr.so"), string.Empty);
            File.WriteAllText(Path.Combine(nativePath, "libcoreclr.so"), string.Empty);
        }
        else
        {
            File.WriteAllText(Path.Combine(libPath, "Microsoft.AspNetCore.App.deps.json"), "{}");
        }
        return packPath;
    }
}
