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
using System.Linq;
using System.Runtime.Versioning;

namespace Microsoft.Azure.Functions.Sdk.Generator.Tests
{
    static class TestHelpers
    {
        // Default language version is the lowest version we support.(C# 7.3 which is default for .NET Framework)
        // See full matrix here: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version
        private const LanguageVersion _defaultLanguageVersion = LanguageVersion.CSharp7_3;

        public static Task RunTestAsync<TSourceGenerator>(
            IEnumerable<Assembly> extensionAssemblyReferences,
            string inputSource,
            string? expectedFileName,
            string? expectedOutputSource,
            List<DiagnosticResult>? expectedDiagnosticResults = null,
            IDictionary<string, string>? buildPropertiesDictionary = null,
            string? generatedCodeNamespace = null,
            LanguageVersion? languageVersion = null,
            bool runInsideAzureFunctionProject = true) where TSourceGenerator : ISourceGenerator, new()
        {
            CSharpSourceGeneratorVerifier<TSourceGenerator>.Test test = new()
            {
                LanguageVersion = languageVersion ?? _defaultLanguageVersion,
                TestState =
                {
                    Sources = { inputSource },
                    AdditionalReferences =
                    {
                        typeof(WorkerExtensionStartupAttribute).Assembly
                    }
                }
            };

            if (expectedOutputSource != null && expectedFileName != null)
            {
                test.TestState.GeneratedSources.Add((typeof(TSourceGenerator), expectedFileName, SourceText.From(expectedOutputSource, Encoding.UTF8)));
            }

            // Enable SourceGen & Placeholder MSBuild Properties for testing
            var config = $@"is_global = true
                            build_property.FunctionsEnableExecutorSourceGen = {true}
                            build_property.FunctionsEnableMetadataSourceGen = {true}
                            build_property.FunctionsGeneratedCodeNamespace = {generatedCodeNamespace ?? "TestProject"}";

            if (runInsideAzureFunctionProject)
            {
                config += $@"
                            build_property.FunctionsExecutionModel = isolated";
            }

            // Add test specific MSBuild properties.
            if (buildPropertiesDictionary is not null)
            {
                foreach (var buildProperty in buildPropertiesDictionary)
                {
                    config += $@"
                                {buildProperty.Key} = {buildProperty.Value}";
                }
            }


            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));

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
        public class Test : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
        {
            public Test()
            {
                var targetFrameworkAttribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                                                       .SingleOrDefault() as TargetFrameworkAttribute;

                string targetFramework = targetFrameworkAttribute!.FrameworkName;
                var tfm = ConvertFrameworkMonikerToTfm(targetFramework);

                this.ReferenceAssemblies = new ReferenceAssemblies(
                    targetFramework: tfm,
                    referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", Environment.Version.ToString()),
                    referenceAssemblyPath: Path.Combine("ref", tfm));
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

            /// <summary>
            /// Example input: .NETCoreApp,Version=v7.0
            /// Example output: net7.0
            /// </summary>
            private static string ConvertFrameworkMonikerToTfm(string frameworkMoniker)
            {
                var parts = frameworkMoniker.Split(',');
                var identifier = parts[0];
                var version = parts[1].Split('=')[1];

                switch (identifier)
                {
                    case ".NETCoreApp":
                        return $"net{version.Substring(1)}";
                    case ".NETFramework":
                        return $"net{version.Replace(".", "")}";
                    case ".NETStandard":
                        return $"netstandard{version.Substring(1)}";
                    default:
                        throw new NotSupportedException($"Unknown framework identifier: {identifier}");
                }
            }
        }
    }
}
