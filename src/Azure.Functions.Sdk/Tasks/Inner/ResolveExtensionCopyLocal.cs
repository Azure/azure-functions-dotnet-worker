// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks.Inner;

/// <summary>
/// Resolves the set of extension assemblies that should be copied locally,
/// excluding those that are part of the Azure Functions runtime.
/// </summary>
public class ResolveExtensionCopyLocal : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the runtime assemblies.
    /// </summary>
    /// <remarks>
    /// These are the assemblies that are part of the Azure Functions runtime and should not be included
    /// in the extensions payload.
    /// </remarks>
    [Required]
    public ITaskItem[] RuntimeAssemblies { get; set; } = [];

    /// <summary>
    /// Gets or sets the runtime packages.
    /// </summary>
    /// <remarks>
    /// These are packages that are part of the Azure Functions runtime and should not be included
    /// in the extensions payload.
    /// </remarks>
    [Required]
    public ITaskItem[] RuntimePackages { get; set; } = [];

    /// <summary>
    /// Gets or sets the copy local files.
    /// </summary>
    [Required]
    public ITaskItem[] CopyLocalFiles { get; set; } = [];

    /// <summary>
    /// Gets the extensions copy local items.
    /// </summary>
    [Output]
    public ITaskItem[] ExtensionsCopyLocal { get; private set; } = [];

    public override bool Execute()
    {
        HashSet<string> runtimeAssemblies = new(
            RuntimeAssemblies.Select(p => p.ItemSpec), StringComparer.OrdinalIgnoreCase);
        HashSet<string> runtimePackages = new(
            RuntimePackages.Select(p => p.ItemSpec), StringComparer.OrdinalIgnoreCase);

        List<ITaskItem> extensionsCopyLocal = [];
        foreach (ITaskItem item in CopyLocalFiles)
        {
            if (ShouldIncludeItem(item, runtimeAssemblies, runtimePackages))
            {
                string destination = item.GetMetadata("DestinationSubPath");
                item.SetMetadata("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, destination));
                extensionsCopyLocal.Add(item);
            }
        }

        ExtensionsCopyLocal = [.. extensionsCopyLocal];
        return !Log.HasLoggedErrors;
    }

    private static bool ShouldIncludeItem(
        ITaskItem item, HashSet<string> runtimeAssemblies, HashSet<string> runtimePackages)
    {
        if (item.TryGetNuGetPackageId(out string? packageId) && runtimePackages.Contains(packageId))
        {
            // Comes from a runtime package, exclude.
            return false;
        }

        // Check if the assembly name is in the runtime assemblies list.
        string fileName = Path.GetFileName(item.ItemSpec);
        return !runtimeAssemblies.Contains(fileName);
    }
}
