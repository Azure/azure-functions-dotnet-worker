﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using FunctionMethodSyntaxReceiver = Microsoft.Azure.Functions.Worker.Sdk.Generators.FunctionMetadataProviderGenerator.FunctionMethodSyntaxReceiver;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    [Generator]
    public sealed partial class FunctionExecutorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FunctionMethodSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!ShouldExecuteGeneration(context))
            {
                return;
            }

            if (context.SyntaxReceiver is not FunctionMethodSyntaxReceiver receiver)
            {
                return;
            }

            var entryAssemblyFuncs = GetSymbolsMethodSyntaxes(receiver.CandidateMethods, context);
            var dependentFuncs = GetDependentAssemblyFunctionsSymbols(context);
            var allMethods = entryAssemblyFuncs.Concat(dependentFuncs);

            if (!allMethods.Any())
            {
                return;
            }

            var parser = new Parser(context);
            var functions = parser.GetFunctions(allMethods);
            var shouldIncludeAutoGeneratedAttributes = ShouldIncludeAutoGeneratedAttributes(context);

            var text = Emitter.Emit(context, functions, shouldIncludeAutoGeneratedAttributes);
            context.AddSource(Constants.FileNames.GeneratedFunctionExecutor, SourceText.From(text, Encoding.UTF8));
        }

        private IEnumerable<IMethodSymbol> GetSymbolsMethodSyntaxes(List<MethodDeclarationSyntax> methods, GeneratorExecutionContext context)
        {
            foreach (MethodDeclarationSyntax method in methods)
            {
                var model = context.Compilation.GetSemanticModel(method.SyntaxTree);

                if (FunctionsUtil.IsValidFunctionMethod(context, context.Compilation, model, method))
                {
                    IMethodSymbol? methodSymbol = (IMethodSymbol)model.GetDeclaredSymbol(method)!;
                    yield return methodSymbol;
                }
            }
        }

        /// <summary>
        /// Collect methods with Function attributes on them from dependent/referenced assemblies.
        /// </summary>
        private IEnumerable<IMethodSymbol> GetDependentAssemblyFunctionsSymbols(GeneratorExecutionContext context)
        {
            var visitor = new ReferencedAssemblyMethodVisitor(context.Compilation);
            visitor.Visit(context.Compilation.SourceModule);

            return visitor.FunctionMethods;
        }
        private static bool ShouldIncludeAutoGeneratedAttributes(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                    Constants.BuildProperties.AutoRegisterGeneratedFunctionsExecutor, out var value))
            {
                return false;
            }

            return string.Equals(value, bool.TrueString, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldExecuteGeneration(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                    Constants.BuildProperties.EnablePlaceholder, out var value))
            {
                return false;
            }

            return string.Equals(value, bool.TrueString, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
