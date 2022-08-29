// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Generates a class with a method which has code to call the "Configure" method
    /// of each of the participating extension's "WorkerExtensionStartup" implementations.
    /// Also adds the assembly attribute "WorkerExtensionStartupCodeExecutorInfo"
    /// and pass the information(the type) about the class we generated.
    /// We are also inheriting the generated class from the WorkerExtensionStartup class.
    /// (This is the same abstract class extension authors will implement for their extension specific startup code)
    /// We need the same signature as the extension's implementation as our class is an uber class which internally
    /// calls each of the extension's implementations.
    /// </summary>

    // Sample code generated (with one extensions participating in startup hook)
    // There will be one try-catch block for each extension participating in startup hook.

    //[assembly: WorkerExtensionStartupCodeExecutorInfo(typeof(Microsoft.Azure.Functions.Worker.WorkerExtensionStartupCodeExecutor))]
    //
    //internal class WorkerExtensionStartupCodeExecutor : WorkerExtensionStartup
    //{
    //    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    //    {
    //        try
    //        {
    //            new Microsoft.Azure.Functions.Worker.Extensions.Http.MyHttpExtensionStartup().Configure(applicationBuilder);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.Error.WriteLine("Error calling Configure on Microsoft.Azure.Functions.Worker.Extensions.Http.MyHttpExtensionStartup instance." + ex.ToString());
    //        }
    //    }
    //}
    [Generator]
    public partial class ExtensionStartupRunnerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get the sub set of assemblies with assembly level startup attribute
            IncrementalValuesProvider<IAssemblySymbol> assembliesValueProvider = context.CompilationProvider
                .SelectMany(static (compilation, token) =>
                    compilation.SourceModule.ReferencedAssemblySymbols
                        .Where(symbol => Parser.IsAssemblyWithExtensionStartupAttribute(symbol, compilation, token)));

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<IAssemblySymbol> AssemblySymbols)>
                compilationWithAssemblies
                    = context.CompilationProvider.Combine(assembliesValueProvider.Collect());

            // Build a data model for the startup type info & any potential diagnostic errors from the assembly list subset.
            IncrementalValuesProvider<StartupTypeInfo> startupTypeInfoProvider = compilationWithAssemblies.SelectMany(
                static (tuple, token) => tuple.AssemblySymbols.Select(assemblySymbol =>
                    Parser.CreateStartupTypeInfoFromAssemblySymbol(tuple.Compilation, assemblySymbol, token)));

            var startupTypeEntries = startupTypeInfoProvider.Collect();

            context.RegisterSourceOutput(startupTypeEntries, Execute);
        }

        private static void Execute(SourceProductionContext context, ImmutableArray<StartupTypeInfo> startupTypes)
        {
            if (startupTypes.IsEmpty)
            {
                return;
            }

            ReportDiagnosticsIfAny(context, startupTypes);

            var startupTypeNames = startupTypes.Where(i => i.Diagnostics.IsEmpty).Select(a => a.StartupTypeName);

            if (!startupTypeNames.Any())
            {
                return;
            }

            SourceText sourceText = Emitter.Emit(startupTypeNames, context.CancellationToken);
            context.AddSource($"WorkerExtensionStartupCodeExecutor.g.cs", sourceText);
        }

        private static void ReportDiagnosticsIfAny(SourceProductionContext context, ImmutableArray<StartupTypeInfo> startupTypeEntries)
        {
            foreach (var startupTypeEntry in startupTypeEntries)
            {
                foreach (var diagnostic in startupTypeEntry.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
