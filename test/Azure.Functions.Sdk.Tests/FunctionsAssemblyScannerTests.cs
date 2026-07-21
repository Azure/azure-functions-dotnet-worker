// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tests;

public class FunctionsAssemblyScannerTests
{
    // Defines the marker attributes in a separate assembly so that, when applied by an extension,
    // they surface as external TypeReferences - exactly like the real WebJobs/Worker attributes.
    // The scanner must match them by metadata name without resolving this assembly, which the tests
    // enforce by deleting it before scanning.
    private const string HostAssemblyName = "TestWebJobsHost";

    private const string HostSource = """
        using System;

        namespace Microsoft.Azure.WebJobs.Hosting
        {
            [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
            public class WebJobsStartupAttribute : Attribute
            {
                public WebJobsStartupAttribute(Type startupType) { }

                public WebJobsStartupAttribute(Type startupType, string name) { }
            }
        }

        namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
        {
            [AttributeUsage(AttributeTargets.Assembly)]
            public sealed class ExtensionInformationAttribute : Attribute
            {
                public ExtensionInformationAttribute(string extensionName, string extensionVersion) { }
            }
        }
        """;

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
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestExtension",
            """
            [assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(TestExtension.FooWebJobsStartup))]

            namespace TestExtension
            {
                public class FooWebJobsStartup { }
            }
            """);

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        WebJobsReference reference = references.Should().ContainSingle().Subject;
        reference.Name.Should().Be("Foo");
        reference.TypeName.Should().StartWith("TestExtension.FooWebJobsStartup, TestExtension");
        reference.HintPath.Should().Be($"./{Constants.ExtensionsOutputFolder}/TestExtension.dll");
    }

    [Fact]
    public void GetWebJobsReferences_UsesExplicitName_WhenProvided()
    {
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestExtension",
            """
            [assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(TestExtension.Startup), "MyExplicitName")]

            namespace TestExtension
            {
                public class Startup { }
            }
            """);

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        references.Should().ContainSingle().Which.Name.Should().Be("MyExplicitName");
    }

    [Fact]
    public void GetWebJobsReferences_ReturnsAll_WhenMultipleStartups()
    {
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestExtension",
            """
            [assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(TestExtension.FirstWebJobsStartup))]
            [assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(TestExtension.SecondWebJobsStartup))]

            namespace TestExtension
            {
                public class FirstWebJobsStartup { }
                public class SecondWebJobsStartup { }
            }
            """);

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        references.Select(r => r.Name).Should().BeEquivalentTo("First", "Second");
    }

    [Fact]
    public void GetWebJobsReferences_DetectsDerivedAttribute_ViaBaseTypeWalk()
    {
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestExtension",
            """
            [assembly: TestExtension.CustomStartup(typeof(TestExtension.BazWebJobsStartup))]

            namespace TestExtension
            {
                using System;
                using Microsoft.Azure.WebJobs.Hosting;

                public class CustomStartupAttribute : WebJobsStartupAttribute
                {
                    public CustomStartupAttribute(Type startupType) : base(startupType) { }
                }

                public class BazWebJobsStartup { }
            }
            """);

        IReadOnlyList<WebJobsReference> references = FunctionsAssemblyScanner.GetWebJobsReferences(assemblyPath);

        WebJobsReference reference = references.Should().ContainSingle().Subject;
        reference.Name.Should().Be("Baz");
        reference.TypeName.Should().StartWith("TestExtension.BazWebJobsStartup, TestExtension");
    }

    [Fact]
    public void GetWebJobsReferences_ReturnsEmpty_WhenNoStartupAttribute()
    {
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestExtension",
            """
            namespace TestExtension
            {
                public class NotAStartup { }
            }
            """);

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
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestWorkerExtension",
            """
            [assembly: Microsoft.Azure.Functions.Worker.Extensions.Abstractions.ExtensionInformation("MyExtension", "1.2.3")]
            """);

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
        using TempDirectory dir = new();
        string assemblyPath = CreateExtensionAssembly(
            dir,
            "TestWorkerExtension",
            """
            namespace TestWorkerExtension
            {
                public class NotAnExtension { }
            }
            """);

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

    // Compiles an extension assembly into the scan directory. The attribute-defining "host" assembly
    // is compiled only as a reference and then deleted, so scanning must succeed without ever
    // resolving it - mirroring how Microsoft.Azure.WebJobs.Host is excluded from the payload.
    private static string CreateExtensionAssembly(TempDirectory scanDir, string assemblyName, string source)
    {
        string hostPath = TestAssemblyCompiler.Compile(
            Path.Combine(scanDir.Path, HostAssemblyName + ".dll"), HostSource);

        string outputPath = TestAssemblyCompiler.Compile(
            Path.Combine(scanDir.Path, assemblyName + ".dll"), source, hostPath);

        File.Delete(hostPath);
        return outputPath;
    }
}
