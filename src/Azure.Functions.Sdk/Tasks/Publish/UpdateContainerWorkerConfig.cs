// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks.Publish;

public sealed class UpdateContainerWorkerConfig(IFileSystem fileSystem) : Microsoft.Build.Utilities.Task
{
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public UpdateContainerWorkerConfig()
        : this(new FileSystem())
    {
    }

    [Required]
    public string StagingPath { get; set; } = string.Empty;

    [Output]
    public bool UsePrivateAppHost { get; private set; }

    [Output]
    public string WorkerPath { get; private set; } = string.Empty;

    [Output]
    public string WorkerAssemblyPath { get; private set; } = string.Empty;

    public override bool Execute()
    {
        string stagingPath = _fileSystem.Path.GetFullPath(StagingPath)
            .TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar)
            + _fileSystem.Path.DirectorySeparatorChar;
        string configPath = _fileSystem.Path.Combine(stagingPath, "worker.config.json");
        if (!_fileSystem.File.Exists(configPath))
        {
            Log.LogError(Strings.Container_WorkerConfigMissing, configPath);
            return false;
        }

        try
        {
            JsonNode? config = JsonNode.Parse(
                _fileSystem.File.ReadAllText(configPath),
                documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (config is not JsonObject root
                || root["description"] is not JsonObject description
                || description["defaultExecutablePath"] is not JsonValue executable
                || !executable.TryGetValue(out string? executablePath))
            {
                Log.LogError(Strings.Container_InvalidWorkerConfig, configPath);
                return false;
            }

            if (!string.Equals(executablePath, "dotnet", StringComparison.Ordinal))
            {
                return true;
            }

            if (description["defaultWorkerPath"] is not JsonValue worker
                || !worker.TryGetValue(out string? workerPath)
                || string.IsNullOrEmpty(workerPath)
                || _fileSystem.Path.IsPathRooted(workerPath)
                || !TryNormalizeWorkerPath(workerPath, out string normalizedWorkerPath))
            {
                Log.LogError(Strings.Container_InvalidWorkerConfig, configPath);
                return false;
            }

            StringComparison pathComparison = _fileSystem.Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            WorkerAssemblyPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(stagingPath, normalizedWorkerPath));
            if (!WorkerAssemblyPath.StartsWith(stagingPath, pathComparison)
                || !_fileSystem.File.Exists(WorkerAssemblyPath))
            {
                Log.LogError(Strings.Container_WorkerAssemblyMissing, workerPath, configPath);
                return false;
            }

            WorkerPath = normalizedWorkerPath;
            description["defaultExecutablePath"] = "{WorkerRoot}.functions-worker";
            // The apphost already selects the DLL; otherwise the host passes it as an application argument.
            description["defaultWorkerPath"] = null;
            _fileSystem.File.WriteAllText(configPath, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            UsePrivateAppHost = true;
            return true;
        }
        catch (JsonException)
        {
            Log.LogError(Strings.Container_InvalidWorkerConfig, configPath);
            return false;
        }
    }

    private static bool TryNormalizeWorkerPath(string path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        path = path.Replace('\\', '/');
        if (path.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        List<string> segments = [];
        foreach (string segment in path.Split('/'))
        {
            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return false;
                }
                segments.RemoveAt(segments.Count - 1);
            }
            else if (segment.Length > 0 && segment != ".")
            {
                segments.Add(segment);
            }
        }

        normalizedPath = string.Join("/", segments);
        return normalizedPath.Length > 0;
    }
}
