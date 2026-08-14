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
