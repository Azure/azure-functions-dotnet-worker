// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Azure.Functions.Sdk.Tasks.Inner.Tests;

public sealed class ResolveExtensionCopyLocalTests
{
    private readonly Mock<IBuildEngine> _buildEngine = new();

    [Fact]
    public void EmptyInputs_ReturnsEmpty()
    {
        ResolveExtensionCopyLocal task = CreateTask();

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void ItemNotInRuntime_Included()
    {
        ITaskItem file = CreateCopyLocalFile("MyExtension.dll");
        ResolveExtensionCopyLocal task = CreateTask(copyLocalFiles: [file]);

        bool result = task.Execute();


        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveItemSpec("MyExtension.dll")
            .And.HaveMetadata("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "MyExtension.dll"));
    }

    [Fact]
    public void ItemInRuntimeAssemblies_Excluded()
    {
        ITaskItem runtimeAssembly = new TaskItem("MyRuntime.dll");
        ITaskItem file = CreateCopyLocalFile("MyRuntime.dll");
        ResolveExtensionCopyLocal task = CreateTask(
            runtimeAssemblies: [runtimeAssembly],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void RuntimeAssemblyMatch_CaseInsensitive()
    {
        ITaskItem runtimeAssembly = new TaskItem("myruntime.dll");
        ITaskItem file = CreateCopyLocalFile("MYRUNTIME.DLL");
        ResolveExtensionCopyLocal task = CreateTask(
            runtimeAssemblies: [runtimeAssembly],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void ItemInRuntimePackage_Excluded()
    {
        ITaskItem runtimePackage = new TaskItem("Microsoft.Azure.Functions.Worker");
        TaskItem file = CreateCopyLocalFile("SomeAssembly.dll");
        file.NuGetPackageId = "Microsoft.Azure.Functions.Worker";
        ResolveExtensionCopyLocal task = CreateTask(
            runtimePackages: [runtimePackage],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void RuntimePackageMatch_CaseInsensitive()
    {
        ITaskItem runtimePackage = new TaskItem("microsoft.azure.functions.worker");
        TaskItem file = CreateCopyLocalFile("SomeAssembly.dll");
        file.NuGetPackageId = "Microsoft.Azure.Functions.Worker";
        ResolveExtensionCopyLocal task = CreateTask(
            runtimePackages: [runtimePackage],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void ItemNotInRuntimePackage_Included()
    {
        ITaskItem runtimePackage = new TaskItem("Microsoft.Azure.Functions.Worker");
        TaskItem file = CreateCopyLocalFile("MyExtension.dll");
        file.NuGetPackageId = "My.Custom.Extension";
        ResolveExtensionCopyLocal task = CreateTask(
            runtimePackages: [runtimePackage],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveItemSpec("MyExtension.dll")
            .And.HaveMetadata("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "MyExtension.dll"));
    }

    [Fact]
    public void ItemWithNoPackageId_NotInRuntimeAssemblies_Included()
    {
        ITaskItem runtimePackage = new TaskItem("Microsoft.Azure.Functions.Worker");
        ITaskItem file = CreateCopyLocalFile("MyExtension.dll");
        ResolveExtensionCopyLocal task = CreateTask(
            runtimePackages: [runtimePackage],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveItemSpec("MyExtension.dll")
            .And.HaveMetadata("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "MyExtension.dll"));
    }

    [Fact]
    public void IncludedItem_TargetPathSet()
    {
        ITaskItem file = CreateCopyLocalFile("MyExtension.dll");
        ResolveExtensionCopyLocal task = CreateTask(copyLocalFiles: [file]);

        task.Execute();

        string expected = Path.Combine(".azurefunctions", "MyExtension.dll");
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveMetadata("TargetPath", expected);
    }

    [Fact]
    public void IncludedItem_WithDestinationSubPath_UsesDestinationSubPath()
    {
        TaskItem file = CreateCopyLocalFile("MyExtension.dll", "sub/MyExtension.dll");
        ResolveExtensionCopyLocal task = CreateTask(copyLocalFiles: [file]);

        task.Execute();

        string expected = Path.Combine(".azurefunctions", "sub/MyExtension.dll");
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveMetadata("TargetPath", expected);
    }

    [Fact]
    public void RuntimeAssemblyMatch_UsesFileNameOnly()
    {
        ITaskItem runtimeAssembly = new TaskItem("MyRuntime.dll");
        ITaskItem file = CreateCopyLocalFile("packages/lib/net8.0/MyRuntime.dll");
        ResolveExtensionCopyLocal task = CreateTask(
            runtimeAssemblies: [runtimeAssembly],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void MixedItems_OnlyNonRuntimeIncluded()
    {
        ITaskItem runtimeAssembly = new TaskItem("Runtime.dll");
        ITaskItem runtimePackage = new TaskItem("Runtime.Package");

        TaskItem file1 = CreateCopyLocalFile("Runtime.dll");
        TaskItem file2 = CreateCopyLocalFile("Extension.dll");
        TaskItem file3 = CreateCopyLocalFile("PackageLib.dll");
        file3.NuGetPackageId = "Runtime.Package";
        TaskItem file4 = CreateCopyLocalFile("AnotherExtension.dll");
        file4.NuGetPackageId = "Other.Package";

        ResolveExtensionCopyLocal task = CreateTask(
            runtimeAssemblies: [runtimeAssembly],
            runtimePackages: [runtimePackage],
            copyLocalFiles: [file1, file2, file3, file4]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().HaveCount(2);
        task.ExtensionsCopyLocal[0].Should().HaveItemSpec("Extension.dll")
            .And.HaveMetadata("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "Extension.dll"));
        task.ExtensionsCopyLocal[1].Should().HaveItemSpec("AnotherExtension.dll")
            .And.HaveMetadata("TargetPath", Path.Combine(Constants.ExtensionsOutputFolder, "AnotherExtension.dll"));
    }

    [Fact]
    public void RuntimePackageTakesPrecedence_OverAssemblyNameNotMatching()
    {
        // Even if the assembly name is NOT in runtimeAssemblies, if the package is in runtimePackages,
        // it should be excluded.
        ITaskItem runtimePackage = new TaskItem("Runtime.Package");
        TaskItem file = CreateCopyLocalFile("UniqueAssembly.dll");
        file.NuGetPackageId = "Runtime.Package";

        ResolveExtensionCopyLocal task = CreateTask(
            runtimePackages: [runtimePackage],
            copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void IncludedItem_WithTargetPath_UsesTargetPath()
    {
        TaskItem file = new TaskItem("MyExtension.dll");
        file.SetMetadata("TargetPath", "custom/MyExtension.dll");
        ResolveExtensionCopyLocal task = CreateTask(copyLocalFiles: [file]);

        task.Execute();

        string expected = Path.Combine(".azurefunctions", "custom/MyExtension.dll");
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveMetadata("TargetPath", expected);
    }

    [Fact]
    public void IncludedItem_WithRootedTargetPath_Excluded()
    {
        TaskItem file = new TaskItem("MyExtension.dll");
        file.SetMetadata("TargetPath", Path.Combine(Path.GetTempPath(), "MyExtension.dll"));
        ResolveExtensionCopyLocal task = CreateTask(copyLocalFiles: [file]);

        bool result = task.Execute();

        result.Should().BeTrue();
        task.ExtensionsCopyLocal.Should().BeEmpty();
    }

    [Fact]
    public void IncludedItem_TargetPathTakesPrecedenceOverDestinationSubPath()
    {
        TaskItem file = new TaskItem("MyExtension.dll");
        file.SetMetadata("TargetPath", "fromTargetPath/MyExtension.dll");
        file.SetMetadata("DestinationSubPath", "fromDestSub/MyExtension.dll");
        ResolveExtensionCopyLocal task = CreateTask(copyLocalFiles: [file]);

        task.Execute();

        string expected = Path.Combine(".azurefunctions", "fromTargetPath/MyExtension.dll");
        task.ExtensionsCopyLocal.Should().ContainSingle()
            .Which.Should().HaveMetadata("TargetPath", expected);
    }

    private static TaskItem CreateCopyLocalFile(string itemSpec, string? destinationSubPath = null)
    {
        TaskItem item = new TaskItem(itemSpec);
        if (destinationSubPath != null)
        {
            item.SetMetadata("DestinationSubPath", destinationSubPath);
        }

        return item;
    }

    private ResolveExtensionCopyLocal CreateTask(
        ITaskItem[]? runtimeAssemblies = null,
        ITaskItem[]? runtimePackages = null,
        ITaskItem[]? copyLocalFiles = null)
    {
        return new()
        {
            BuildEngine = _buildEngine.Object,
            RuntimeAssemblies = runtimeAssemblies ?? [],
            RuntimePackages = runtimePackages ?? [],
            CopyLocalFiles = copyLocalFiles ?? [],
        };
    }
}
