// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.Azure.Functions.Worker.Core;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    static class TestHelpers
    {
        public static Task RunTestAsync<TSourceGenerator>(
            IEnumerable<Assembly> extensionAssemblyReferences,
            string inputSource,
            string? expectedFileName,
            string? expectedOutputSource,
            List<DiagnosticResult>? expectedDiagnosticResults = null) where TSourceGenerator : ISourceGenerator, new()
        {
            CSharpSourceGeneratorVerifier<TSourceGenerator>.Test test = new()
            {
                TestState =
                {
                    Sources = { inputSource },
                    AdditionalReferences =
                    {
                        typeof(WorkerExtensionStartupAttribute).Assembly,
                    },
                },
            };

            if (expectedOutputSource != null && expectedFileName != null)
            {
                test.TestState.GeneratedSources.Add((typeof(TSourceGenerator), expectedFileName, SourceText.From(expectedOutputSource, Encoding.UTF8)));
            }

            foreach (var item in extensionAssemblyReferences)
            {
                test.TestState.AdditionalReferences.Add(item);
            }

            if (expectedDiagnosticResults != null)
            {
                test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnosticResults);
            }

            return test.RunAsync();
        }

    }

    // Mostly copy/pasted from the Microsoft Source Generators testing documentation
    // https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators
    public static class CSharpSourceGeneratorVerifier<TSourceGenerator> where TSourceGenerator : ISourceGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
        {
            //REF: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core

            public Test()
            {
#if NET7_0_OR_GREATER //NOTE: EOL Support May 14, 2024

                    // See https://www.nuget.org/packages/Microsoft.NETCore.App.Ref/7.0.1
                    this.ReferenceAssemblies = new ReferenceAssemblies(
                        targetFramework: "net7.0",
                        referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "7.0.1"),
                        referenceAssemblyPath: Path.Combine("ref", "net7.0"));

#elif NET6_0_OR_GREATER //NOTE: EOL Support November 12, 2024

                    // See https://www.nuget.org/packages/Microsoft.NETCore.App.Ref/6.0.0
                    this.ReferenceAssemblies = new ReferenceAssemblies(
                        targetFramework: "net6.0",
                        referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "6.0.0"),
                        referenceAssemblyPath: Path.Combine("ref", "net6.0"));

#elif NET5_0_OR_GREATER //NOTE: No longer supported since May 10, 2022

                    // See https://www.nuget.org/packages/Microsoft.NETCore.App.Ref/5.0.0
                    this.ReferenceAssemblies = new ReferenceAssemblies(
                        targetFramework: "net5.0",
                        referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "5.0.0"),
                        referenceAssemblyPath: Path.Combine("ref", "net5.0"));

#elif NETCOREAPP3_1_OR_GREATER  //NOTE: No longer supported since December 13, 2022

                    // See https://www.nuget.org/packages/Microsoft.NETCore.App.Ref/3.1.0
                    this.ReferenceAssemblies = new ReferenceAssemblies(
                        targetFramework: "netcoreapp3.1",
                        referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "3.1.0"),
                        referenceAssemblyPath: Path.Combine("ref", "netcoreapp3.1"));
#else
                    throw new InvalidOperationException("TFM is no longer supported by this library.");
#endif
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp9;

            protected override CompilationOptions CreateCompilationOptions()
            {
                CompilationOptions compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                     compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
            }

            static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
            {
                string[] args = { "/warnaserror:nullable" };
                var commandLineArguments = CSharpCommandLineParser.Default.Parse(
                    args,
                    baseDirectory: Environment.CurrentDirectory,
                    sdkDirectory: Environment.CurrentDirectory);
                var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

                return nullableWarnings;
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(this.LanguageVersion);
            }
        }
    }
}
