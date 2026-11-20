// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Azure.Functions.Sdk.Tests;

public class FunctionsAssemblyScannerTests
{
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
    public void GetWebJobsReferences_FunctionsStartupAttributeWithoutAttributeAssembly_IsDiscovered()
    {
        using TempDirectory directory = new();
        string assemblyPath = Path.Combine(directory.Path, "FunctionsStartupConsumer.dll");
        CreateStartupConsumerAssembly(assemblyPath);
        FunctionsAssemblyScanner scanner = new();

        WebJobsReference reference = scanner.GetWebJobsReferences(assemblyPath).Single();

        reference.Name.Should().Be("ExternalFunctionsStartup");
        reference.TypeName.Should().StartWith("Azure.Functions.Sdk.Tests.ExternalFunctionsStartup, FunctionsStartupConsumer");
        reference.HintPath.Should().Be("./.azurefunctions/FunctionsStartupConsumer.dll");
    }

    [Fact]
    public void GetWebJobsReferences_WebJobsStartupAttribute_UsesProvidedName()
    {
        using TempDirectory directory = new();
        string assemblyPath = Path.Combine(directory.Path, "WebJobsStartupConsumer.dll");
        CreateStartupConsumerAssembly(assemblyPath, "CustomExtension");
        FunctionsAssemblyScanner scanner = new();

        WebJobsReference reference = scanner.GetWebJobsReferences(assemblyPath).Single();

        reference.Name.Should().Be("CustomExtension");
        reference.TypeName.Should().StartWith("Azure.Functions.Sdk.Tests.ExternalFunctionsStartup, WebJobsStartupConsumer");
        reference.HintPath.Should().Be("./.azurefunctions/WebJobsStartupConsumer.dll");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetWebJobsReferences_ReleasesAssemblyAndResolvedDependencies(bool invalidAttribute)
    {
        using TempDirectory directory = new();
        string assemblyPath = Path.Combine(directory.Path, "Consumer.dll");
        string dependencyPath = Path.Combine(directory.Path, "CustomAttributes.dll");
        CreateDerivedStartupConsumerAssembly(assemblyPath, dependencyPath, invalidAttribute);
        FunctionsAssemblyScanner scanner = new([directory.Path]);

        if (invalidAttribute)
        {
            Action scan = () => scanner.GetWebJobsReferences(assemblyPath).ToArray();
            scan.Should().Throw<InvalidOperationException>();
        }
        else
        {
            IEnumerable<WebJobsReference> references = scanner.GetWebJobsReferences(assemblyPath);
            using (File.Open(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (File.Open(dependencyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                references.Single().Name.Should().Be("ExternalFunctionsStartup");
            }
        }

        using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using FileStream dependencyFile = File.Open(dependencyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private static void CreateDerivedStartupConsumerAssembly(
        string assemblyPath, string dependencyPath, bool invalidAttribute)
    {
        using AssemblyDefinition dependency = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("CustomAttributes", new Version(1, 0)),
            "CustomAttributes",
            ModuleKind.Dll);
        ModuleDefinition dependencyModule = dependency.MainModule;
        AssemblyNameReference functionsExtensions = new("Microsoft.Azure.Functions.Extensions", new Version(1, 0));
        dependencyModule.AssemblyReferences.Add(functionsExtensions);
        TypeReference baseAttribute = new(
            "Microsoft.Azure.Functions.Extensions.DependencyInjection", "FunctionsStartupAttribute",
            dependencyModule, functionsExtensions);
        TypeDefinition derivedAttribute = new(
            "Custom", "DerivedStartupAttribute",
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Class, baseAttribute);
        dependencyModule.Types.Add(derivedAttribute);
        dependency.Write(dependencyPath);

        using AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("Consumer", new Version(1, 0)), "Consumer", ModuleKind.Dll);
        ModuleDefinition module = assembly.MainModule;
        TypeDefinition startupType = new(
            "Azure.Functions.Sdk.Tests", "ExternalFunctionsStartup",
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(startupType);
        MethodReference constructor = new(".ctor", module.TypeSystem.Void, module.ImportReference(derivedAttribute))
        {
            HasThis = true,
        };
        CustomAttribute attribute = new(constructor);
        if (!invalidAttribute)
        {
            TypeReference systemType = module.ImportReference(typeof(Type));
            constructor.Parameters.Add(new ParameterDefinition(systemType));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(systemType, startupType));
        }

        assembly.CustomAttributes.Add(attribute);
        assembly.Write(assemblyPath);
    }

    private static void CreateStartupConsumerAssembly(string assemblyPath, string? extensionName = null)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(Path.GetFileNameWithoutExtension(assemblyPath), new Version(1, 0)),
            Path.GetFileNameWithoutExtension(assemblyPath),
            ModuleKind.Dll);
        ModuleDefinition module = assembly.MainModule;
        TypeDefinition startupType = new(
            "Azure.Functions.Sdk.Tests",
            "ExternalFunctionsStartup",
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Class,
            module.TypeSystem.Object);
        module.Types.Add(startupType);

        bool isFunctionsStartup = extensionName is null;
        AssemblyNameReference attributeAssembly = new(
            isFunctionsStartup ? "Microsoft.Azure.Functions.Extensions" : "Microsoft.Azure.WebJobs.Host",
            new Version(1, 0));
        module.AssemblyReferences.Add(attributeAssembly);
        TypeReference attributeType = new(
            isFunctionsStartup
                ? "Microsoft.Azure.Functions.Extensions.DependencyInjection"
                : "Microsoft.Azure.WebJobs.Hosting",
            isFunctionsStartup ? "FunctionsStartupAttribute" : "WebJobsStartupAttribute",
            module,
            attributeAssembly);
        MethodReference constructor = new(".ctor", module.TypeSystem.Void, attributeType)
        {
            HasThis = true,
        };
        TypeReference systemType = module.ImportReference(typeof(Type));
        constructor.Parameters.Add(new ParameterDefinition(systemType));
        CustomAttribute attribute = new(constructor);
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(systemType, startupType));
        if (extensionName is not null)
        {
            constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
            attribute.ConstructorArguments.Add(
                new CustomAttributeArgument(module.TypeSystem.String, extensionName));
        }

        assembly.CustomAttributes.Add(attribute);

        assembly.Write(assemblyPath);
    }
}
