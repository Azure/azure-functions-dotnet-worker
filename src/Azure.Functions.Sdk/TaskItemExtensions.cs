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

        /// <summary>
        /// Gets or sets the "NuGetPackageId" metadata on the task item.
        /// </summary>
        public string NuGetPackageId
        {
            get => taskItem.GetMetadata("NuGetPackageId") ?? string.Empty;
            set => taskItem.SetMetadata("NuGetPackageId", value);
        }

        /// <summary>
        /// Gets or sets the "NuGetPackageVersion" metadata on the task item.
        /// </summary>
        public string NuGetPackageVersion
        {
            get => taskItem.GetMetadata("NuGetPackageVersion") ?? string.Empty;
            set => taskItem.SetMetadata("NuGetPackageVersion", value);
        }

        /// <summary>
        /// Tries to get the target path for this extension file to copy to.
        /// </summary>
        /// <param name="targetPath">The target path for the functions output.</param>
        /// <returns><c>true</c> if the target path was successfully resolved; <c>false</c> otherwise.</returns>
        public bool TryGetFunctionsTargetPath(out string targetPath)
        {
            // TODO: Consider support for absolute paths.
            // The logic today is assuming all items have relative paths.

            // 1. If TargetPath is set, use it.
            string target = taskItem.GetMetadata("TargetPath");

            // 2. If TargetPath is null or empty, use DestinationSubPath.
            if (string.IsNullOrEmpty(target))
            {
                target = taskItem.GetMetadata("DestinationSubPath");
            }

            // 3. If DestinationSubPath is null or empty, use Path.GetFileName.
            if (string.IsNullOrEmpty(target))
            {
                target = Path.GetFileName(taskItem.ItemSpec);
            }

            // 4. If path is absolute, reject it.
            if (Path.IsPathRooted(target))
            {
                targetPath = string.Empty;
                return false;
            }

            // 5. Return path combined with Constants.ExtensionsOutputFolder.
            targetPath = Path.Combine(Constants.ExtensionsOutputFolder, target);
            return true;
        }

        /// <summary>
        /// Tries to get the NuGet package ID from the task item.
        /// </summary>
        /// <param name="packageId">The package ID, if found.</param>
        /// <returns><c>true</c> if nuget package ID is found; <c>false</c> otherwise.</returns>
        public bool TryGetNuGetPackageId([NotNullWhen(true)] out string? packageId)
        {
            packageId = taskItem.NuGetPackageId;
            return !string.IsNullOrEmpty(packageId);
        }

        /// <summary>
        /// Tries to get the NuGet package version from the task item.
        /// </summary>
        /// <param name="packageVersion">The package version, if found.</param>
        /// <returns><c>true</c> if nuget package version is found; <c>false</c> otherwise.</returns>
        public bool TryGetNuGetPackageVersion([NotNullWhen(true)] out string? packageVersion)
        {
            packageVersion = taskItem.NuGetPackageVersion;
            return !string.IsNullOrEmpty(packageVersion);
        }
    }
}
