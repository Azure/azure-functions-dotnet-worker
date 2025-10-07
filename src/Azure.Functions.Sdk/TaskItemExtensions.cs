// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk;

public static class TaskItemExtensions
{
    public static string GetVersion(this ITaskItem taskItem)
    {
        return taskItem.GetMetadata("Version") ?? string.Empty;
    }

    public static void SetVersion(this ITaskItem taskItem, string version)
    {
        taskItem.SetMetadata("Version", version);
    }

    public static bool GetIsImplicitlyDefined(this ITaskItem taskItem)
    {
        return bool.TryParse(taskItem.GetMetadata("IsImplicitlyDefined"), out bool isImplicitlyDefined)
            && isImplicitlyDefined;
    }

    public static void SetIsImplicitlyDefined(this ITaskItem taskItem, bool isImplicitlyDefined)
    {
        taskItem.SetMetadata("IsImplicitlyDefined", isImplicitlyDefined.ToString().ToLowerInvariant());
    }

    public static bool TryGetSourcePackageId(
        this ITaskItem taskItem, [NotNullWhen(true)] out string? packageId)
    {
        packageId = taskItem.GetMetadata("SourcePackageId");
        return !string.IsNullOrEmpty(packageId);
    }

    public static void SetSourcePackageId(this ITaskItem taskItem, string sourcePackageId)
    {
        taskItem.SetMetadata("SourcePackageId", sourcePackageId);
    }

    public static bool TryGetNuGetPackageId(
        this ITaskItem taskItem, [NotNullWhen(true)] out string? nuGetPackageId)
    {
        nuGetPackageId = taskItem.GetMetadata("NuGetPackageId");
        return !string.IsNullOrEmpty(nuGetPackageId);
    }

    public static string GetModuleName(this ITaskItem taskItem)
    {
        return taskItem.GetMetadata("FusionName") ?? string.Empty;
    }

    public static bool GetCanTrim(this ITaskItem taskItem)
    {
        return bool.TryParse(taskItem.GetMetadata("CanTrim"), out bool canTrim) && canTrim;
    }

    public static void SetCanTrim(this ITaskItem taskItem, bool canTrim)
    {
        taskItem.SetMetadata("CanTrim", canTrim.ToString().ToLowerInvariant());
    }
}
