// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Azure.Functions.Sdk.Tests;

/// <summary>
/// Compiles small C# source strings into on-disk assemblies so scanner behavior can be tested
/// against real PE metadata without shipping pre-built binary fixtures.
/// </summary>
internal static class TestAssemblyCompiler
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> RuntimeReferences =
        new(CreateRuntimeReferences);

    /// <summary>
    /// Compiles <paramref name="source"/> into a DLL at <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="outputPath">The disk path (including file name) to emit the assembly to. The
    /// file name without extension becomes the assembly identity.</param>
    /// <param name="source">The C# source to compile.</param>
    /// <param name="referencePaths">Optional paths to additional assemblies to reference.</param>
    /// <returns>The <paramref name="outputPath"/> that was written.</returns>
    public static string Compile(string outputPath, string source, params string[] referencePaths)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(outputPath);
        IEnumerable<MetadataReference> references = RuntimeReferences.Value
            .Concat(referencePaths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)));

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        EmitResult result = compilation.Emit(outputPath);
        if (!result.Success)
        {
            string errors = string.Join(
                Environment.NewLine,
                result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException(
                $"Failed to compile test assembly '{assemblyName}':{Environment.NewLine}{errors}");
        }

        return outputPath;
    }

    private static IReadOnlyList<MetadataReference> CreateRuntimeReferences()
    {
        // On .NET (net10) the trusted platform assemblies list gives us the full framework
        // reference set. On .NET Framework (net472) it is unavailable, so fall back to mscorlib,
        // which is sufficient for the trivial attribute types these tests compile.
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa && tpa.Length > 0)
        {
            return [.. tpa.Split(Path.PathSeparator)
                .Where(p => p.Length > 0 && File.Exists(p))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))];
        }

        return [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
    }
}
