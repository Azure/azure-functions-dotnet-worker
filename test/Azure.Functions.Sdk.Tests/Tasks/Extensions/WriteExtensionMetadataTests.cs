// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks.Extensions.Tests;

public class WriteExtensionMetadataTests
{
    private const string ExtensionJsonPath = "extensions.json";
    private readonly MockFileSystem _fileSystem = new();

    private WriteExtensionMetadata CreateTask(ITaskItem[]? extensionReferences = null)
    {
        return new(_fileSystem)
        {
            OutputPath = ExtensionJsonPath,
            ExtensionReferences = extensionReferences ?? [],
            BuildEngine = Mock.Of<IBuildEngine>(),
        };
    }

    [Fact]
    public void Canceled_ReturnsFalse()
    {
        using WriteExtensionMetadata task = CreateTask();
        task.Cancel(); // Cancel before execution
        bool result = task.Execute();
        result.Should().BeFalse();
    }

    [Fact]
    public void NoExtensions_EmptyJson()
    {
        using WriteExtensionMetadata task = CreateTask();
        bool result = task.Execute();

        result.Should().BeTrue();
        ValidateExtensionJson();
    }

    [Fact]
    public void HashMatch_SkipsRun()
    {
        using WriteExtensionMetadata task = CreateTask();
        task.Execute().Should().BeTrue(); // generate hash

        DateTime time = _fileSystem.File.GetLastWriteTimeUtc(ExtensionJsonPath);

        bool result = task.Execute(); // run again, should skip
        result.Should().BeTrue();
        _fileSystem.File.GetLastWriteTimeUtc(ExtensionJsonPath).Should().Be(time);
    }

    [Fact]
    public void HashMismatch_WritesJson()
    {
        using WriteExtensionMetadata task = CreateTask();
        _fileSystem.File.WriteAllText(ExtensionJsonPath, "invalid hash");
        DateTime time = _fileSystem.File.GetLastWriteTimeUtc(ExtensionJsonPath);

        Thread.Sleep(100); // ensure file system timestamp changes
        bool result = task.Execute(); // run again, should regenerate
        result.Should().BeTrue();
        _fileSystem.File.GetLastWriteTimeUtc(ExtensionJsonPath).Should().BeAfter(time);
    }

    private void ValidateExtensionJson(params WebJobsPackage[] expectedPackages)
    {
        _fileSystem.FileExists(ExtensionJsonPath).Should().BeTrue("extensions.json should exist.");
        string extensionsJson = _fileSystem.File.ReadAllText(ExtensionJsonPath);

        WebJobsExtensions? metadata = JsonSerializer.Deserialize<WebJobsExtensions>(extensionsJson);
        metadata.Should().NotBeNull("extensions.json should deserialize correctly.");

        metadata!.Extensions.Should().HaveCount(expectedPackages.Length);
        foreach (WebJobsPackage pkg in expectedPackages)
        {
            metadata.Extensions.Should().ContainSingle(e =>
                string.Equals(e.Name, pkg.ExtensionName, StringComparison.OrdinalIgnoreCase));
        }
    }

    private class WebJobsExtensions
    {
        [JsonPropertyName("extensions")]
        public List<Extension> Extensions { get; set; } = [];

        public class Extension
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
