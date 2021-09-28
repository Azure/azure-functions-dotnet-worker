// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncVoidAnalyzer : DiagnosticAnalyzer
    {    
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AsyncVoidReturnType);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext symbolAnalysisContext)
        {
            var symbol = (IMethodSymbol)symbolAnalysisContext.Symbol;

            if (symbol.IsAsync && symbol.ReturnsVoid)
            {
                // This symbol is a method symbol and will have only one item in Locations property.
                var location = symbol.Locations[0]; 
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.AsyncVoidReturnType, location);
                symbolAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
