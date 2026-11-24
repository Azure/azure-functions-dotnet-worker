// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace Azure.Functions.Sdk.Tasks.Publish;

public sealed class PrepareFunctionsContainer(IFileSystem fileSystem) : Microsoft.Build.Utilities.Task
{
    private const string CoreFramework = "Microsoft.NETCore.App";
    private const string AspNetFramework = "Microsoft.AspNetCore.App";
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

    public string RestoreConfigFile { get; set; } = string.Empty;

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
    public string RuntimeProjectPath { get; private set; } = string.Empty;

    [Output]
    public string RestoreConfigPath { get; private set; } = string.Empty;

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
        RuntimeProjectPath = _fileSystem.Path.Combine(StagingRoot, "runtime-project", "Azure.Functions.Sdk.RuntimePacks.csproj");
        if (RestoreConfigFile != string.Empty)
        {
            RestoreConfigPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(ProjectDirectory, RestoreConfigFile));
        }

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
            return ResolveFrameworks(document.RootElement, runtimeConfigPath);
        }
        catch (JsonException exception)
        {
            Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, exception.Message);
            return false;
        }
    }

    private bool ResolveFrameworks(JsonElement root, string runtimeConfigPath)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("runtimeOptions", out JsonElement options)
            || options.ValueKind != JsonValueKind.Object)
        {
            Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, "runtimeOptions is missing");
            return false;
        }

        List<JsonElement> frameworks = [];
        if (options.TryGetProperty("framework", out JsonElement framework))
        {
            frameworks.Add(framework);
        }

        if (options.TryGetProperty("frameworks", out JsonElement frameworkArray))
        {
            if (frameworkArray.ValueKind != JsonValueKind.Array)
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, "frameworks must be an array");
                return false;
            }

            frameworks.AddRange(frameworkArray.EnumerateArray());
        }

        NuGetVersion? coreVersion = null;
        NuGetVersion? coreRequested = null;
        NuGetVersion? aspNetVersion = null;
        NuGetVersion? aspNetRequested = null;
        bool coreExact = false;
        bool aspNetExact = false;
        foreach (JsonElement entry in frameworks)
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !TryGetString(entry, "name", out string name)
                || !TryGetString(entry, "version", out string version)
                || !NuGetVersion.TryParse(version, out NuGetVersion? requested))
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, "a framework name or version is invalid");
                return false;
            }

            if (name != CoreFramework && name != AspNetFramework)
            {
                Log.LogError(Strings.Container_UnsupportedFramework, name, version);
                return false;
            }

            if ((name == CoreFramework && coreVersion is not null)
                || (name == AspNetFramework && aspNetVersion is not null))
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, $"framework '{name}' is specified more than once");
                return false;
            }

            ITaskItem? knownFramework = KnownFrameworkReferences.FirstOrDefault(item =>
                item.ItemSpec.Equals(name, StringComparison.OrdinalIgnoreCase)
                && item.GetMetadata("TargetFramework").Equals(TargetFramework, StringComparison.OrdinalIgnoreCase));
            if (knownFramework is null
                || !NuGetVersion.TryParse(knownFramework.GetMetadata("LatestRuntimeFrameworkVersion"), out NuGetVersion? latest)
                || requested.Major != latest.Major
                || requested.Minor != latest.Minor)
            {
                Log.LogError(Strings.Container_UnsupportedFramework, name, version);
                return false;
            }

            if (!TryGetRuntimePolicy(entry, options, out bool exactVersion))
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, $"framework '{name}' has an invalid roll-forward policy");
                return false;
            }

            NuGetVersion selected = !exactVersion && latest.CompareTo(requested) >= 0
                && (!latest.IsPrerelease || requested.IsPrerelease) ? latest : requested;
            if (name == CoreFramework)
            {
                coreVersion = selected;
                coreRequested = requested;
                coreExact = exactVersion;
            }
            else
            {
                aspNetVersion = selected;
                aspNetRequested = requested;
                aspNetExact = exactVersion;
            }
        }

        if (coreVersion is null || coreRequested is null)
        {
            Log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, $"framework '{CoreFramework}' is missing");
            return false;
        }

        if (aspNetVersion is not null && aspNetRequested is not null && aspNetVersion.CompareTo(coreVersion) > 0)
        {
            // ASP.NET Core's runtime configuration also requires the corresponding .NET Core runtime.
            if (!coreExact && (!aspNetVersion.IsPrerelease || coreRequested.IsPrerelease))
            {
                coreVersion = aspNetVersion;
            }
            else if (!aspNetExact && aspNetRequested.CompareTo(coreVersion) <= 0
                && (!coreVersion.IsPrerelease || aspNetRequested.IsPrerelease))
            {
                aspNetVersion = coreVersion;
            }
            else
            {
                Log.LogError(Strings.Container_IncompatibleFrameworks, coreVersion, aspNetVersion);
                return false;
            }
        }

        CoreRuntimeVersion = coreVersion.ToNormalizedString();
        AspNetRuntimeVersion = aspNetVersion?.ToNormalizedString() ?? string.Empty;
        return true;
    }

    private static bool TryGetRuntimePolicy(JsonElement framework, JsonElement options, out bool exactVersion)
    {
        exactVersion = false;
        if (TryGetProperty(framework, options, "rollForward", out JsonElement rollForward))
        {
            if (rollForward.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            string? value = rollForward.GetString();
            if (!new[] { "Disable", "LatestPatch", "Minor", "LatestMinor", "Major", "LatestMajor" }
                .Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            exactVersion = string.Equals(value, "Disable", StringComparison.OrdinalIgnoreCase);
        }

        if (TryGetProperty(framework, options, "applyPatches", out JsonElement applyPatches))
        {
            if (applyPatches.ValueKind != JsonValueKind.True && applyPatches.ValueKind != JsonValueKind.False)
            {
                return false;
            }

            exactVersion |= !applyPatches.GetBoolean();
        }

        return true;
    }

    private static bool TryGetProperty(JsonElement framework, JsonElement options, string name, out JsonElement value) =>
        framework.TryGetProperty(name, out value) || options.TryGetProperty(name, out value);

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return value != string.Empty;
    }

    private string DirectoryPath(string path)
    {
        string fullPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(ProjectDirectory, path));
        return fullPath.TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar)
            + _fileSystem.Path.DirectorySeparatorChar;
    }
}
