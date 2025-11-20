// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks.Extensions;

public class ValidateExtensionPackages : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] ExtensionPackages { get; set; } = [];

    [Output]
    public ITaskItem[] FilteredPackages { get; private set; } = [];

    public override bool Execute()
    {
        // key=identity, value=version
        Dictionary<string, ITaskItem> uniquePackages = new(StringComparer.OrdinalIgnoreCase);
        foreach (ITaskItem package in ExtensionPackages)
        {
            if (uniquePackages.TryGetValue(package.ItemSpec, out ITaskItem? existingPackage))
            {
                // Duplicate package detected.
                // Log a warning and continue if versions match.
                // Log an error if versions do not match.
                string version = existingPackage.Version;
                string newVersion = package.Version;
                if (version != newVersion)
                {
                    Log.LogMessage(LogMessage.Error_ExtensionPackageConflict, package.ItemSpec, version, newVersion);
                }
                else
                {
                    Log.LogMessage(LogMessage.Warning_ExtensionPackageDuplicate, package.ItemSpec, version);
                }
            }
            else
            {
                // Check version is a valid nuget package version.
                string version = package.Version;
                if (NuGet.Versioning.NuGetVersion.TryParse(version, out _))
                {
                    uniquePackages[package.ItemSpec] = package;
                }
                else
                {
                    Log.LogMessage(LogMessage.Error_InvalidExtensionPackageVersion, package.ItemSpec, version);
                }
            }
        }

        FilteredPackages = [.. uniquePackages.Values];
        return !Log.HasLoggedErrors;
    }
}
