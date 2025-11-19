// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for <see cref="ITaskItem"/>.
/// </summary>
public static class TaskItemExtensions
{
    /// <summary>
    /// Gets the "Version" metadata from the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <returns>The version metadata, or empty string if not present.</returns>
    public static string GetVersion(this ITaskItem taskItem)
    {
        return taskItem.GetMetadata("Version") ?? string.Empty;
    }

    /// <summary>
    /// Sets the "Version" metadata on the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <param name="version">The version to set.</param>
    public static void SetVersion(this ITaskItem taskItem, string version)
    {
        taskItem.SetMetadata("Version", version);
    }

    /// <summary>
    /// Gets the "IsImplicitlyDefined" metadata from the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <returns><c>true</c> if implicitly defined, <c>false</c> otherwise.</returns>
    public static bool GetIsImplicitlyDefined(this ITaskItem taskItem)
    {
        return bool.TryParse(taskItem.GetMetadata("IsImplicitlyDefined"), out bool isImplicitlyDefined)
            && isImplicitlyDefined;
    }

    /// <summary>
    /// Sets the "IsImplicitlyDefined" metadata on the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <param name="isImplicitlyDefined">The value to set.</param>
    public static void SetIsImplicitlyDefined(this ITaskItem taskItem, bool isImplicitlyDefined)
    {
        taskItem.SetMetadata("IsImplicitlyDefined", isImplicitlyDefined.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Gets the "SourcePackageId" metadata from the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <param name="packageId">The package ID, if found.</param>
    /// <returns><c>true</c> if "SourcePackageId" is found, <c>false</c> if not found.</returns>
    public static bool TryGetSourcePackageId(
        this ITaskItem taskItem, [NotNullWhen(true)] out string? packageId)
    {
        packageId = taskItem.GetMetadata("SourcePackageId");
        return !string.IsNullOrEmpty(packageId);
    }

    /// <summary>
    /// Sets the "SourcePackageId" metadata on the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <param name="sourcePackageId">The source package ID to set.</param>
    public static void SetSourcePackageId(this ITaskItem taskItem, string sourcePackageId)
    {
        taskItem.SetMetadata("SourcePackageId", sourcePackageId);
    }
}
