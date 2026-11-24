// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace Azure.Functions.Sdk.Tasks.Publish;

public sealed class GetFunctionsContainerRuntimeDownloads(IFileSystem fileSystem) : Microsoft.Build.Utilities.Task
{
    internal const string CorePackage = "Microsoft.NETCore.App.Runtime.linux-x64";
    internal const string AspNetPackage = "Microsoft.AspNetCore.App.Runtime.linux-x64";
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public GetFunctionsContainerRuntimeDownloads()
        : this(new FileSystem())
    {
    }

    [Required]
    public string TargetFramework { get; set; } = string.Empty;

    [Required]
    public ITaskItem[] KnownFrameworkReferences { get; set; } = [];

    [Required]
    public ITaskItem[] RuntimeFrameworks { get; set; } = [];

    public ITaskItem[] ExistingPackageDownloads { get; set; } = [];

    public string RollForward { get; set; } = string.Empty;

    public string UserRuntimeConfig { get; set; } = string.Empty;

    [Output]
    public ITaskItem[] PackageDownloads { get; private set; } = [];

    public override bool Execute()
    {
        JsonObject? core = CreateFramework(FunctionsContainerRuntimeVersionResolver.CoreFramework);
        JsonObject? aspNet = CreateFramework(FunctionsContainerRuntimeVersionResolver.AspNetFramework);
        if (core is null || aspNet is null)
        {
            return false;
        }

        JsonObject options = new()
        {
            ["frameworks"] = new JsonArray(core, aspNet),
        };
        if (RollForward != string.Empty)
        {
            options["rollForward"] = RollForward;
        }

        if (!ApplyUserRuntimeConfig(options))
        {
            return false;
        }

        FunctionsContainerRuntimeVersionResolver resolver = new(KnownFrameworkReferences, TargetFramework, Log);
        using JsonDocument both = JsonDocument.Parse(new JsonObject { ["runtimeOptions"] = options.DeepClone() }.ToJsonString());
        if (!resolver.TryResolve(both.RootElement, UserRuntimeConfig, out string coreVersion, out string aspNetVersion))
        {
            return false;
        }

        // A transitive ASP.NET framework reference is only known after restore. Also acquire the
        // versions needed when the app references only Microsoft.NETCore.App.
        JsonObject coreOnlyOptions = (JsonObject)options.DeepClone();
        JsonArray frameworks = (JsonArray)coreOnlyOptions["frameworks"]!;
        for (int i = frameworks.Count - 1; i >= 0; i--)
        {
            if (frameworks[i] is JsonObject entry
                && string.Equals((string?)entry["name"], FunctionsContainerRuntimeVersionResolver.AspNetFramework, StringComparison.OrdinalIgnoreCase))
            {
                frameworks.RemoveAt(i);
            }
        }

        using JsonDocument coreOnly = JsonDocument.Parse(new JsonObject { ["runtimeOptions"] = coreOnlyOptions }.ToJsonString());
        if (!resolver.TryResolve(coreOnly.RootElement, UserRuntimeConfig, out string coreOnlyVersion, out _))
        {
            return false;
        }

        PackageDownloads =
        [
            CreateDownload(CorePackage, coreVersion, coreOnlyVersion),
            CreateDownload(AspNetPackage, aspNetVersion, coreOnlyVersion),
        ];
        return true;
    }

    private JsonObject? CreateFramework(string name)
    {
        ITaskItem? framework = RuntimeFrameworks.FirstOrDefault(item =>
            item.ItemSpec.Equals(name, StringComparison.OrdinalIgnoreCase)
            && item.GetMetadata("FrameworkName").Equals(name, StringComparison.OrdinalIgnoreCase));
        if (framework is null || !NuGetVersion.TryParse(framework.GetMetadata("Version"), out _))
        {
            Log.LogError(Strings.Container_RuntimeFrameworkNotResolved, name, TargetFramework);
            return null;
        }

        return new JsonObject
        {
            ["name"] = name,
            ["version"] = framework.GetMetadata("Version"),
        };
    }

    private bool ApplyUserRuntimeConfig(JsonObject options)
    {
        if (UserRuntimeConfig == string.Empty || !_fileSystem.File.Exists(UserRuntimeConfig))
        {
            return true;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(_fileSystem.File.ReadAllText(UserRuntimeConfig),
                documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException exception)
        {
            Log.LogError(Strings.Container_InvalidRuntimeConfig, UserRuntimeConfig, exception.Message);
            return false;
        }

        if (node is not JsonObject template)
        {
            Log.LogError(Strings.Container_InvalidRuntimeConfig, UserRuntimeConfig, "the runtime configuration template must be an object");
            return false;
        }

        foreach (string policy in new[] { "rollForward", "applyPatches" })
        {
            if (template.TryGetPropertyValue(policy, out JsonNode? value))
            {
                options[policy] = value?.DeepClone();
            }
        }

        List<JsonNode?> entries = [];
        if (template.TryGetPropertyValue("framework", out JsonNode? framework))
        {
            entries.Add(framework);
        }
        if (template.TryGetPropertyValue("frameworks", out JsonNode? frameworkArray))
        {
            if (frameworkArray is not JsonArray array)
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, UserRuntimeConfig, "frameworks must be an array");
                return false;
            }
            entries.AddRange(array);
        }

        JsonArray frameworks = (JsonArray)options["frameworks"]!;
        HashSet<string> updated = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonNode? entry in entries)
        {
            if (entry is not JsonObject frameworkEntry
                || frameworkEntry["name"] is not JsonValue nameValue
                || !nameValue.TryGetValue(out string? name)
                || string.IsNullOrEmpty(name))
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, UserRuntimeConfig, "a framework name or version is invalid");
                return false;
            }

            if (!name.Equals(FunctionsContainerRuntimeVersionResolver.CoreFramework, StringComparison.OrdinalIgnoreCase)
                && !name.Equals(FunctionsContainerRuntimeVersionResolver.AspNetFramework, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!updated.Add(name!))
            {
                Log.LogError(Strings.Container_InvalidRuntimeConfig, UserRuntimeConfig, $"framework '{name}' is specified more than once");
                return false;
            }

            bool replaced = false;
            for (int i = 0; i < frameworks.Count; i++)
            {
                if (frameworks[i] is JsonObject known && string.Equals((string?)known["name"], name, StringComparison.OrdinalIgnoreCase))
                {
                    frameworks[i] = frameworkEntry.DeepClone();
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
            {
                frameworks.Add(frameworkEntry.DeepClone());
            }
        }

        return true;
    }

    private ITaskItem CreateDownload(string packageId, params string[] requiredVersions)
    {
        ITaskItem[] existing = ExistingPackageDownloads
            .Where(item => item.ItemSpec.Equals(packageId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        TaskItem download = existing.Length == 0 ? new TaskItem(packageId) : new TaskItem(existing[0]);
        List<string> versions = existing
            .SelectMany(item => item.GetMetadata("Version").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        foreach (string version in requiredVersions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            NuGetVersion parsed = NuGetVersion.Parse(version);
            if (!versions.Any(value => VersionRange.TryParse(value, out VersionRange? range) && range.Satisfies(parsed)))
            {
                versions.Add($"[{version}]");
            }
        }
        download.SetMetadata("Version", string.Join(";", versions));
        return download;
    }
}
