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
    extension(ITaskItem taskItem)
    {
        /// <summary>
        /// Gets the "Version" metadata from the task item.
        /// </summary>
        /// <param name="taskItem">The task item.</param>
        /// <returns>The version metadata, or empty string if not present.</returns>
        public string GetVersion()
        {
            return taskItem.GetMetadata("Version") ?? string.Empty;
        }

        /// <summary>
        /// Sets the "Version" metadata on the task item.
        /// </summary>
        /// <param name="taskItem">The task item.</param>
        /// <param name="version">The version to set.</param>
        public void SetVersion(string version)
        {
            taskItem.SetMetadata("Version", version);
        }

        /// <summary>
        /// Gets the "IsImplicitlyDefined" metadata from the task item.
        /// </summary>
        /// <param name="taskItem">The task item.</param>
        /// <returns><c>true</c> if implicitly defined, <c>false</c> otherwise.</returns>
        public bool GetIsImplicitlyDefined()
        {
            return bool.TryParse(taskItem.GetMetadata("IsImplicitlyDefined"), out bool isImplicitlyDefined)
                && isImplicitlyDefined;
        }

        /// <summary>
        /// Sets the "IsImplicitlyDefined" metadata on the task item.
        /// </summary>
        /// <param name="taskItem">The task item.</param>
        /// <param name="isImplicitlyDefined">The value to set.</param>
        public void SetIsImplicitlyDefined(bool isImplicitlyDefined)
        {
            taskItem.SetMetadata("IsImplicitlyDefined", isImplicitlyDefined.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Gets the "SourcePackageId" metadata from the task item.
        /// </summary>
        /// <param name="taskItem">The task item.</param>
        /// <param name="packageId">The package ID, if found.</param>
        /// <returns><c>true</c> if "SourcePackageId" is found, <c>false</c> if not found.</returns>
        public bool TryGetSourcePackageId([NotNullWhen(true)] out string? packageId)
        {
            packageId = taskItem.GetMetadata("SourcePackageId");
            return !string.IsNullOrEmpty(packageId);
        }

        /// <summary>
        /// Sets the "SourcePackageId" metadata on the task item.
        /// </summary>
        /// <param name="taskItem">The task item.</param>
        /// <param name="sourcePackageId">The source package ID to set.</param>
        public void SetSourcePackageId(string sourcePackageId)
        {
            taskItem.SetMetadata("SourcePackageId", sourcePackageId);
        }
    }
}
