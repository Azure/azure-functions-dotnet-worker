// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for <see cref="ITaskItem"/>.
/// </summary>
public static class TaskItemExtensions
{
    extension(ITaskItem taskItem)
    {
        /// <summary>
        /// Gets or sets the "Version" metadata on the task item.
        /// </summary>
        public string Version
        {
            get => taskItem.GetMetadata("Version") ?? string.Empty;
            set => taskItem.SetMetadata("Version", value);
        }

        /// <summary>
        /// Gets or sets the "IsImplicitlyDefined" metadata on the task item.
        /// </summary>
        public bool IsImplicitlyDefined
        {
            get => bool.TryParse(taskItem.GetMetadata("IsImplicitlyDefined"), out bool isImplicitlyDefined)
                && isImplicitlyDefined;
            set => taskItem.SetMetadata("IsImplicitlyDefined", value.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Gets or sets the "SourcePackageId" metadata on the task item.
        /// </summary>
        public string SourcePackageId
        {
            get => taskItem.GetMetadata("SourcePackageId") ?? string.Empty;
            set => taskItem.SetMetadata("SourcePackageId", value);
        }
    }

    /// <summary>
    /// Gets the "NuGetPackageId" metadata from the task item.
    /// </summary>
    /// <param name="taskItem">The task item.</param>
    /// <param name="packageId">The package ID, if found.</param>
    /// <returns><c>true</c> if "NuGetPackageId" is found, <c>false</c> if not found.</returns>
    public static bool TryGetNuGetPackageId(
        this ITaskItem taskItem, [NotNullWhen(true)] out string? packageId)
    {
        packageId = taskItem.GetMetadata("NuGetPackageId");
        return !string.IsNullOrEmpty(packageId);
    }
}
