using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests.Helpers
{
    internal static class SourceGeneratorVerifyExtensions
    {
        private const LanguageVersion _defaultLanguageVersion = LanguageVersion.CSharp7_3;

        private static readonly string _dotNetAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        private static readonly TimeSpan _executionMaxTime = TimeSpan.FromSeconds(30);

        public static async Task RunAndVerify(
            this IIncrementalGenerator sourceGenerator,
            string inputSource,
            IEnumerable<Assembly>? extensionAssemblyReferences = null,
            IDictionary<string, string>? buildPropertiesDictionary = null,
            string? generatedCodeNamespace = null,
            LanguageVersion? languageVersion = null,
            bool runInsideAzureFunctionProject = true,
            [CallerFilePath] string callerFileName = "",
            [CallerMemberName] string callerName = "")
        {
            using var cts = new CancellationTokenSource();
#if !DEBUG
            cts.CancelAfter(_executionMaxTime);
#endif

            var compilation = CreateCompilation(inputSource, extensionAssemblyReferences, languageVersion);

            var config = CreateAnalyzerOptions(
                buildPropertiesDictionary,
                generatedCodeNamespace,
                runInsideAzureFunctionProject);

            cts.Token.ThrowIfCancellationRequested();
            var driver = CSharpGeneratorDriver.Create(sourceGenerator)
                .WithUpdatedAnalyzerConfigOptions(config);

            var generateResult = driver.RunGenerators(compilation, cts.Token);

            cts.Token.ThrowIfCancellationRequested();
            await VerifyGeneratedCode(generateResult, callerFileName, callerName);
        }

        public static async Task RunAndVerify(
            this ISourceGenerator sourceGenerator,
            string inputSource,
            IEnumerable<Assembly>? extensionAssemblyReferences = null,
            IDictionary<string, string>? buildPropertiesDictionary = null,
            string? generatedCodeNamespace = null,
            LanguageVersion? languageVersion = null,
            bool runInsideAzureFunctionProject = true,
            [CallerFilePath] string callerFileName = "",
            [CallerMemberName] string callerName = "")
        {
            using var cts = new CancellationTokenSource();
#if !DEBUG
            cts.CancelAfter(_executionMaxTime);
#endif

            var compilation = CreateCompilation(inputSource, extensionAssemblyReferences, languageVersion);

            var config = CreateAnalyzerOptions(
                buildPropertiesDictionary,
                generatedCodeNamespace,
                runInsideAzureFunctionProject);

            cts.Token.ThrowIfCancellationRequested();
            var driver = CSharpGeneratorDriver.Create(sourceGenerator)
                .WithUpdatedAnalyzerConfigOptions(config);

            var generateResult = driver.RunGenerators(compilation, cts.Token);

            cts.Token.ThrowIfCancellationRequested();
            await VerifyGeneratedCode(generateResult, callerFileName, callerName);
        }

        private static async Task VerifyGeneratedCode(GeneratorDriver generateResult, string callerFileName, string callerName)
        {
            var settings = Verifier
                .Verify(generateResult)
                .DisableRequireUniquePrefix();

            if (!string.IsNullOrWhiteSpace(callerFileName))
            {
                settings = settings
                    .UseDirectory(Path.GetDirectoryName(callerFileName))
                    .UseFileName($"{Path.GetFileNameWithoutExtension(callerFileName)}.{callerName}");
            }

            await settings;
        }

        private static AnalyzerConfigOptions CreateAnalyzerOptions(
            IDictionary<string, string>? buildPropertiesDictionary,
            string? generatedCodeNamespace,
            bool runInsideAzureFunctionProject)
        {
            var options = new Dictionary<string, string>()
            {
                ["is_global"] = true.ToString(),
                ["build_property.FunctionsEnableExecutorSourceGen"] = true.ToString(),
                ["build_property.FunctionsEnableMetadataSourceGen"] = true.ToString(),
                ["build_property.FunctionsGeneratedCodeNamespace"] = generatedCodeNamespace ?? "TestProject"
            };

            if (runInsideAzureFunctionProject)
            {
                options.Add("build_property.FunctionsExecutionModel", "isolated");
            }

            if (buildPropertiesDictionary is not null)
            {
                foreach (var pair in buildPropertiesDictionary)
                {
                    options[pair.Key] = pair.Value;
                }
            }

            var config = new AnalyzerConfigOptions(options);
            return config;
        }

        private static CSharpCompilation CreateCompilation(string inputSource, IEnumerable<Assembly>? extensionAssemblyReferences, LanguageVersion? languageVersion)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                            inputSource,
                            new CSharpParseOptions(languageVersion ?? _defaultLanguageVersion));

            var metadata = GetAllAssemblies((extensionAssemblyReferences ?? Array.Empty<Assembly>())
                .Concat(new[]
                {
                    typeof(WorkerExtensionStartupAttribute).Assembly,
                    typeof(HttpTriggerAttribute).Assembly,
                    typeof(FunctionAttribute).Assembly
                }))
                .Distinct()
                .Select(l => MetadataReference.CreateFromFile(l))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "TestProject",
                new[] { syntaxTree },
                metadata);

            var errors = compilation.GetDiagnostics()
                .Where(d => d.DefaultSeverity >= DiagnosticSeverity.Error
                    && !GetIgnoredErrors().Contains(d.Id))
                .ToArray();

            Assert.Empty(errors);
            return compilation;
        }

        private static IEnumerable<string> GetAllAssemblies(
            IEnumerable<Assembly> assemblies)
        {
            foreach (var item in assemblies)
            {
                yield return item.Location;

                foreach (var nestedAssembly in GetAllAssemblies(item
                    .GetReferencedAssemblies()
                    .Select(x => Assembly.Load(x))))
                {
                    yield return nestedAssembly;
                }
            }

            foreach (var item in GetBaseCompilationAssemblies())
            {
                yield return item;
            }
        }

        private static IEnumerable<string> GetBaseCompilationAssemblies()
        {
            yield return Path.Combine(_dotNetAssemblyPath, "netstandard.dll");
            yield return Path.Combine(_dotNetAssemblyPath, "System.dll");
            yield return Path.Combine(_dotNetAssemblyPath, "System.Core.dll");
            yield return Path.Combine(_dotNetAssemblyPath, "System.Private.CoreLib.dll");
            yield return Path.Combine(_dotNetAssemblyPath, "System.Runtime.dll");
        }

        private static IEnumerable<string> GetIgnoredErrors()
        {
            yield return "CS5001"; // Program does not contain a static 'Main' method suitable for an entry point
        }
    }
}
