﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Generates a class that implements IFunctionMetadataProvider and the method GetFunctionsMetadataAsync() which returns a list of IFunctionMetadata. 
    /// This source generator indexes a Function App and explicitly creates a list of DefaultFunctionMetadata (which implements IFunctionMetadata) from the functions defined
    /// in the user's compilation. This allows the worker to index functions at build time, rather than waiting for the process to start.
    /// </summary>
    [Generator]
    public partial class FunctionMetadataProviderGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not FunctionMethodSyntaxReceiver receiver || receiver.CandidateMethods.Count == 0)
            {
                return;
            }

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(Constants.BuildProperties.EnableSourceGen, out var sourceGenSwitch);

            bool.TryParse(sourceGenSwitch, out bool enableSourceGen);

            if (!enableSourceGen)
            {
                return;
            }

            // attempt to parse user compilation
            var p = new Parser(context);

            var entryAssemblyFuncs = GetEntryAssemblyFunctions(receiver.CandidateMethods, context);
            var dependentFuncs = GetDependentAssemblyFunctions(context);

            IReadOnlyList<GeneratorFunctionMetadata> functionMetadataInfo = p.GetFunctionMetadataInfo(entryAssemblyFuncs.Concat(dependentFuncs).ToList());

            // Proceed to generate the file if function metadata info was successfully returned
            if (functionMetadataInfo.Count > 0)
            {
                Emitter e = new();
                var shouldIncludeAutoGeneratedAttributes = ShouldIncludeAutoGeneratedAttributes(context);

                string result = e.Emit(functionMetadataInfo, shouldIncludeAutoGeneratedAttributes, context.CancellationToken);

                context.AddSource(Constants.FileNames.GeneratedFunctionMetadata, SourceText.From(result, Encoding.UTF8));
            }
        }

        /// <summary>
        /// Register a factory that can create our custom syntax receiver
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FunctionMethodSyntaxReceiver());
        }
        
        private static bool ShouldIncludeAutoGeneratedAttributes(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                    Constants.BuildProperties.AutoRegisterGeneratedMetadataProvider, out var value))
            {
                return false;
            }

            return string.Equals(value, bool.TrueString, System.StringComparison.OrdinalIgnoreCase);
        }

        private IList<IMethodSymbol> GetEntryAssemblyFunctions(List<MethodDeclarationSyntax> candidateMethods, GeneratorExecutionContext context)
        {
            IList<IMethodSymbol>? entryAssemblyFuncs = new List<IMethodSymbol>();

            foreach (MethodDeclarationSyntax method in candidateMethods)
            {
                var model = context.Compilation.GetSemanticModel(method.SyntaxTree);

                if (FunctionsUtil.IsValidFunctionMethod(context, context.Compilation, model,  method))
                {
                    IMethodSymbol? methodSymbol = (IMethodSymbol) model.GetDeclaredSymbol(method)!;
                    entryAssemblyFuncs.Add(methodSymbol);
                }
            }

            return entryAssemblyFuncs.Count > 0 ? entryAssemblyFuncs : ImmutableList<IMethodSymbol>.Empty;
        }

        /// <summary>
        /// Collect methods with Function attributes on them from dependent/referenced assemblies.
        /// </summary>
        private IList<IMethodSymbol> GetDependentAssemblyFunctions(GeneratorExecutionContext context)
        {
            IList<IMethodSymbol>? dependentAssemblyFuncs = new List<IMethodSymbol>();

            foreach (var assembly in context.Compilation.SourceModule.ReferencedAssemblySymbols)
            {
                var namespaceSymbols = assembly.GlobalNamespace.GetMembers();

                foreach (var namespaceSymbol in namespaceSymbols)
                {
                    var namespaceMembers = namespaceSymbol.GetMembers();

                    foreach (var m in namespaceMembers)
                    {
                        if (m is INamedTypeSymbol namedType)
                        {
                            var typeMembers = namedType.GetMembers();

                            foreach (var typeMember in typeMembers)
                            {
                                if (typeMember is IMethodSymbol method)
                                {
                                    if (FunctionsUtil.IsFunctionSymbol(method, context.Compilation))
                                    {
                                        dependentAssemblyFuncs.Add(method);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dependentAssemblyFuncs.Count > 0 ? dependentAssemblyFuncs : ImmutableList<IMethodSymbol>.Empty;
        }
    }
}
