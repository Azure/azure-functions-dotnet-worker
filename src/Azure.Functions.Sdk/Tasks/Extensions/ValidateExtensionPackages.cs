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
        List<ITaskItem> uniquePackages = [];
        foreach (IGrouping<string, ITaskItem> packages in ExtensionPackages.GroupBy(p => p.TargetFramework))
        {
            if (string.IsNullOrEmpty(packages.Key))
            {
                string packageList = string.Join($"{Environment.NewLine}  ", packages.Select(p => p.ItemSpec));
                Log.LogMessage(
                    LogMessage.Warning_ExtensionPackageTargetFrameworkMissing,
                    packageList);
            }
            else
            {
                uniquePackages.AddRange(Validate(packages));
            }
        }

        FilteredPackages = [.. uniquePackages];
        return !Log.HasLoggedErrors;
    }

    private IEnumerable<ITaskItem> Validate(IEnumerable<ITaskItem> extensionPackages)
    {
        // Validate that there are no duplicate packages with different versions.
        // This can occur when multiple projects reference the same extension with different versions.
        // We want to catch this and log an error, as it will cause runtime failures.
        Dictionary<string, ITaskItem> uniquePackages = new(StringComparer.OrdinalIgnoreCase);
        foreach (ITaskItem package in extensionPackages)
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

        return uniquePackages.Values;
    }
}
