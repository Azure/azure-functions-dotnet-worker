// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tests;

public class FunctionsAssemblyScannerTests
{
    // Sample extension assemblies are built in place by the projects under
    // test/Resources/AssemblyScanner (referenced with ReferenceOutputAssembly=false) and scanned
    // directly from their own build output directories, located here by relative-path convention.
    // Each extension references the real WebJobs / Worker.Extensions.Abstractions packages that
    // define the marker attributes, but those package assemblies do NOT sit in the sample's output
    // directory, so scanning succeeds only because the scanner matches attributes by metadata name
    // without resolving the defining assembly - mirroring how Microsoft.Azure.WebJobs.Host is
    // excluded from the extension payload in production.
    //
    // The test assembly runs from <repo>/test/Azure.Functions.Sdk.Tests/bin/<Config>/<tfm>, and each
    // sample builds to <repo>/test/Resources/AssemblyScanner/<Name>/bin/<Config>/netstandard2.0. Both
    // share the same <Config>, so we anchor at the test assembly's real output directory, read the
    // configuration from it, and walk up to the shared test root.
    private const string SampleTargetFramework = "netstandard2.0";

    private static readonly string TestOutputDirectory = Path.GetDirectoryName(GetAssemblyLocation())!;

    private static readonly string Configuration =
        Path.GetFileName(Path.GetDirectoryName(TestOutputDirectory))!;

    private static readonly string SampleExtensionsRoot = Path.GetFullPath(
        Path.Combine(TestOutputDirectory, "..", "..", "..", "..", "Resources", "AssemblyScanner"));

    private static string GetAssemblyLocation()
    {
#if NET
        return typeof(FunctionsAssemblyScannerTests).Assembly.Location;
#else
        // On net472 the test host shadow-copies the assembly, so Assembly.Location points at the
        // shadow-copy cache, which is not under the repo. CodeBase resolves to the original build
        // output directory that the relative-path convention is anchored on.
        Uri uri = new Uri(typeof(FunctionsAssemblyScannerTests).Assembly.CodeBase!);
        return uri.LocalPath;
#endif
    }

    public static TheoryData<string> ShouldScanPackageFalseData =>
        new()
        {
            "System.Text.Json",
            "Azure.Core",
            "Azure.Identity",
            "Microsoft.Bcl.AsyncInterfaces",
            "Microsoft.Extensions.Azure",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Identity.Client",
            "Microsoft.NETCore.Platforms",
            "Microsoft.NETStandard.Library",
            "Microsoft.Win32.Registry",
            "Grpc.AspNetCore",
            "Grpc.Net.Client",
        };

    public static TheoryData<string> ShouldScanPackageTrueData =>
        new()
        {
            "Azure.CoreOther",
            "SystemSomethingElse",
            "Microsoft.BclOther",
            "Microsoft.Azure.WebJobs.Host.Storage",
            "Microsoft.Azure.Functions.Worker.Extensions",
            "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus",
            "Some.Custom.Package",
        };

    public static TheoryData<string?> NullOrEmptyAssemblyPath =>
        new() { null, string.Empty };

    [Theory]
    [MemberData(nameof(ShouldScanPackageFalseData))]
    public void ShouldScanPackage_ReturnsFalse(string assembly)
    {
        bool result = FunctionsAssemblyScanner.ShouldScanPackage(assembly);
        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ShouldScanPackageTrueData))]
    public void ShouldScanPackage_ReturnsTrue(string assembly)
    {
        bool result = FunctionsAssemblyScanner.ShouldScanPackage(assembly);
        result.Should().BeTrue();
    }

    [Fact]
    public void GetWebJobsReferences_DetectsStartup_WithoutResolvingAttributeAssembly()
    {
        string assemblyPath = ExtensionPath("TestExtension.Startup");

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        WebJobsReference reference = references.Should().ContainSingle().Subject;
        reference.Name.Should().Be("Foo");
        reference.TypeName.Should().StartWith("TestExtension.FooWebJobsStartup, TestExtension.Startup");
        reference.HintPath.Should().Be($"./{Constants.ExtensionsOutputFolder}/TestExtension.Startup.dll");
    }

    [Fact]
    public void GetWebJobsReferences_UsesExplicitName_WhenProvided()
    {
        string assemblyPath = ExtensionPath("TestExtension.NamedStartup");

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        references.Should().ContainSingle().Which.Name.Should().Be("MyExplicitName");
    }

    [Fact]
    public void GetWebJobsReferences_ReturnsAll_WhenMultipleStartups()
    {
        string assemblyPath = ExtensionPath("TestExtension.MultipleStartups");

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        references.Select(r => r.Name).Should().BeEquivalentTo("First", "Second");
    }

    [Fact]
    public void GetWebJobsReferences_DetectsDerivedAttribute_ViaBaseTypeWalk()
    {
        string assemblyPath = ExtensionPath("TestExtension.DerivedStartup");

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        WebJobsReference reference = references.Should().ContainSingle().Subject;
        reference.Name.Should().Be("Baz");
        reference.TypeName.Should().StartWith("TestExtension.BazWebJobsStartup, TestExtension.DerivedStartup");
    }

    [Fact]
    public void GetWebJobsReferences_ReturnsEmpty_WhenNoStartupAttribute()
    {
        string assemblyPath = ExtensionPath("TestExtension.Plain");

        FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyAssemblyPath))]
    public void GetWebJobsReferences_Throws_WhenAssemblyPathIsNullOrEmpty(string? assemblyPath)
    {
        Action act = () => FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetExtensionReference_ReturnsTrue_WithoutResolvingAttributeAssembly()
    {
        string assemblyPath = ExtensionPath("TestExtension.Information");

        bool result = FunctionsAssemblyScanner.TryGetExtensionReference(
            assemblyPath, "Some.Package.Id", out ITaskItem? extensionReference);

        result.Should().BeTrue();
        ITaskItem reference = extensionReference!;
        reference.ItemSpec.Should().Be("MyExtension");
        reference.Version.Should().Be("1.2.3");
        reference.SourcePackageId.Should().Be("Some.Package.Id");
        reference.IsImplicitlyDefined.Should().BeTrue();
    }

    [Fact]
    public void TryGetExtensionReference_ReturnsFalse_WhenNoAttribute()
    {
        string assemblyPath = ExtensionPath("TestExtension.Plain");

        bool result = FunctionsAssemblyScanner.TryGetExtensionReference(
            assemblyPath, "Some.Package.Id", out ITaskItem? extensionReference);

        result.Should().BeFalse();
        ((object?)extensionReference).Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyAssemblyPath))]
    public void TryGetExtensionReference_Throws_WhenAssemblyPathIsNullOrEmpty(string? assemblyPath)
    {
        Action act = () => FunctionsAssemblyScanner.TryGetExtensionReference(assemblyPath!, "pkg", out _);
        act.Should().Throw<ArgumentException>();
    }

    private static string ExtensionPath(string assemblyName) => Path.Combine(
        SampleExtensionsRoot, assemblyName, "bin", Configuration, SampleTargetFramework, assemblyName + ".dll");
}
