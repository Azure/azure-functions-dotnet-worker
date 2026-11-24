// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace Azure.Functions.Sdk.Tasks.Publish;

internal sealed class FunctionsContainerRuntimeVersionResolver(
    ITaskItem[] knownFrameworkReferences,
    string targetFramework,
    TaskLoggingHelper log)
{
    internal const string CoreFramework = "Microsoft.NETCore.App";
    internal const string AspNetFramework = "Microsoft.AspNetCore.App";

    public bool TryResolve(JsonElement root, string runtimeConfigPath, out string coreRuntimeVersion, out string aspNetRuntimeVersion)
    {
        coreRuntimeVersion = string.Empty;
        aspNetRuntimeVersion = string.Empty;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("runtimeOptions", out JsonElement options)
            || options.ValueKind != JsonValueKind.Object)
        {
            log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, "runtimeOptions is missing");
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
                log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, "frameworks must be an array");
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
                log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, "a framework name or version is invalid");
                return false;
            }

            if (name != CoreFramework && name != AspNetFramework)
            {
                log.LogError(Strings.Container_UnsupportedFramework, name, version);
                return false;
            }

            if ((name == CoreFramework && coreVersion is not null)
                || (name == AspNetFramework && aspNetVersion is not null))
            {
                log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, $"framework '{name}' is specified more than once");
                return false;
            }

            ITaskItem? knownFramework = FindKnownFramework(name);
            if (knownFramework is null
                || !NuGetVersion.TryParse(knownFramework.GetMetadata("LatestRuntimeFrameworkVersion"), out NuGetVersion? latest)
                || requested.Major != latest.Major
                || requested.Minor != latest.Minor)
            {
                log.LogError(Strings.Container_UnsupportedFramework, name, version);
                return false;
            }

            if (!TryGetRuntimePolicy(entry, options, out bool exactVersion))
            {
                log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, $"framework '{name}' has an invalid roll-forward policy");
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
            log.LogError(Strings.Container_InvalidRuntimeConfig, runtimeConfigPath, $"framework '{CoreFramework}' is missing");
            return false;
        }

        if (aspNetVersion is null)
        {
            ITaskItem? knownAspNet = FindKnownFramework(AspNetFramework);
            if (knownAspNet is null
                || !NuGetVersion.TryParse(knownAspNet.GetMetadata("LatestRuntimeFrameworkVersion"), out NuGetVersion? latest)
                || latest.Major != coreVersion.Major
                || latest.Minor != coreVersion.Minor)
            {
                log.LogError(Strings.Container_UnsupportedFramework, AspNetFramework, coreVersion);
                return false;
            }

            aspNetVersion = coreVersion;
        }
        else if (aspNetRequested is not null && aspNetVersion.CompareTo(coreVersion) > 0)
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
                log.LogError(Strings.Container_IncompatibleFrameworks, coreVersion, aspNetVersion);
                return false;
            }
        }

        coreRuntimeVersion = coreVersion.ToNormalizedString();
        aspNetRuntimeVersion = aspNetVersion.ToNormalizedString();
        return true;
    }

    private ITaskItem? FindKnownFramework(string name) =>
        knownFrameworkReferences.FirstOrDefault(item =>
            item.ItemSpec.Equals(name, StringComparison.OrdinalIgnoreCase)
            && item.GetMetadata("TargetFramework").Equals(targetFramework, StringComparison.OrdinalIgnoreCase));

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
}
