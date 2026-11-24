// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks.Publish;

public sealed class PrepareFunctionsContainer(IFileSystem fileSystem) : Microsoft.Build.Utilities.Task
{
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public PrepareFunctionsContainer()
        : this(new FileSystem())
    {
    }

    [Required]
    public string ProjectDirectory { get; set; } = string.Empty;

    [Required]
    public string PublishDirectory { get; set; } = string.Empty;

    [Required]
    public string IntermediateOutputPath { get; set; } = string.Empty;

    [Required]
    public string RuntimeConfigFileName { get; set; } = string.Empty;

    [Required]
    public string TargetFramework { get; set; } = string.Empty;

    [Required]
    public ITaskItem[] KnownFrameworkReferences { get; set; } = [];

    [Output]
    public string PublishedPath { get; private set; } = string.Empty;

    [Output]
    public string StagingRoot { get; private set; } = string.Empty;

    [Output]
    public string StagingPath { get; private set; } = string.Empty;

    [Output]
    public string CoreRuntimeVersion { get; private set; } = string.Empty;

    [Output]
    public string AspNetRuntimeVersion { get; private set; } = string.Empty;

    public override bool Execute()
    {
        StringComparison pathComparison = _fileSystem.Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        PublishedPath = DirectoryPath(PublishDirectory);
        StagingRoot = DirectoryPath(_fileSystem.Path.Combine(
            IntermediateOutputPath, "azure_functions_container", Guid.NewGuid().ToString("N")));
        StagingPath = DirectoryPath(_fileSystem.Path.Combine(StagingRoot, "publish"));
        if (PublishedPath.StartsWith(StagingRoot, pathComparison)
            || StagingRoot.StartsWith(PublishedPath, pathComparison))
        {
            Log.LogError(Strings.Container_OverlappingPaths, PublishedPath, StagingRoot);
            return false;
        }

        string runtimeConfigPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(PublishedPath, RuntimeConfigFileName));
        if (!runtimeConfigPath.StartsWith(PublishedPath, pathComparison)
            || !_fileSystem.File.Exists(runtimeConfigPath))
        {
            Log.LogError(Strings.Container_RuntimeConfigMissing, runtimeConfigPath);
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                _fileSystem.File.ReadAllText(runtimeConfigPath),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            FunctionsContainerRuntimeVersionResolver resolver = new(KnownFrameworkReferences, TargetFramework, Log);
            bool resolved = resolver.TryResolve(document.RootElement, runtimeConfigPath, out string coreVersion, out string aspNetVersion);
            CoreRuntimeVersion = coreVersion;
            AspNetRuntimeVersion = aspNetVersion;
            return resolved;
        }
        catch (JsonException exception)
        {
            Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, exception.Message);
            return false;
        }
    }

    private string DirectoryPath(string path)
    {
        string fullPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(ProjectDirectory, path));
        return fullPath.TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar)
            + _fileSystem.Path.DirectorySeparatorChar;
    }
}
