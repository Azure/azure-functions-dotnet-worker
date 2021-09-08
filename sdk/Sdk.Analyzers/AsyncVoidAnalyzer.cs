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
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.AsyncVoidReturnType, symbol.Locations[0]);
                symbolAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
