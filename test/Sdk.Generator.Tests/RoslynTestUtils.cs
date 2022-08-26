// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    /// <summary>
    /// This is a trimmed down version of the TestUtils from the dotnet runtime repo.
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/SourceGenerators/RoslynTestUtils.cs
    /// </summary>
    internal static class RoslynTestUtils
    {
        /// <summary>
        /// Runs a Roslyn generator over a set of source files.
        /// </summary>
        public static async Task<(ImmutableArray<Diagnostic>, ImmutableArray<GeneratedSourceResult>)> RunGenerator(
            IIncrementalGenerator generator,
            IEnumerable<Assembly>? references,
            IEnumerable<string> sources,
            bool includeBaseReferences = true,
            CancellationToken cancellationToken = default)
        {
            Project proj = CreateTestProject(references, includeBaseReferences);
            proj = proj.WithDocuments(sources);
            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));
            Compilation? comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
            return RunGenerator(comp!, generator, cancellationToken);
        }
        
        /// <summary>
        /// Creates a canonical Roslyn project for testing.
        /// </summary>
        /// <param name="references">Assembly references to include in the project.</param>
        /// <param name="includeBaseReferences">Whether to include references to the BCL assemblies.</param>
        private static Project CreateTestProject(IEnumerable<Assembly>? references, bool includeBaseReferences = true)
        {
            string core = Assembly.GetAssembly(typeof(object))!.Location;
            string runtimeDir = Path.GetDirectoryName(core)!;

            var refs = new List<MetadataReference>();
            if (includeBaseReferences)
            {
                refs.Add(MetadataReference.CreateFromFile(core));
                refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));
                refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
            }

            if (references != null)
            {
                foreach (var r in references)
                {
                    refs.Add(MetadataReference.CreateFromFile(r.Location));
                }
            }

            return new AdhocWorkspace()
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("Test", "test.dll", "C#")
                .WithMetadataReferences(refs)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
        }
        
        private static Project WithDocuments(this Project project, IEnumerable<string> sources, IEnumerable<string>? sourceNames = null)
        {
            int count = 0;
            Project result = project;
            if (sourceNames != null)
            {
                List<string> names = sourceNames.ToList();
                foreach (string s in sources)
                {
                    result = result.WithDocument(names[count++], s);
                }
            }
            else
            {
                foreach (string s in sources)
                {
                    result = result.WithDocument($"src-{count++}.cs", s);
                }
            }

            return result;
        }

        private static Project WithDocument(this Project proj, string name, string text)
        {
            return proj.AddDocument(name, text).Project;
        }
        
        /// <summary>
        /// Runs a Roslyn generator given a Compilation.
        /// </summary>
        private static (ImmutableArray<Diagnostic>, ImmutableArray<GeneratedSourceResult>) RunGenerator(
            Compilation comp,
            IIncrementalGenerator generator,
            CancellationToken cancellationToken = default)
        {
            CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
            CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: options);
            GeneratorDriver gd = cgd.RunGenerators(comp!, cancellationToken);

            GeneratorDriverRunResult r = gd.GetRunResult();
            return (r.Results[0].Diagnostics, r.Results[0].GeneratedSources);
        }
    }
}
