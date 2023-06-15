﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DeferredBindingAttributeNotSupported : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.DeferredBindingAttributeNotSupported);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.NamedType);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext symbolAnalysisContext)
        {
            var symbol = (INamedTypeSymbol)symbolAnalysisContext.Symbol;

            var attributes = symbol.GetAttributes();

            if (attributes.IsEmpty)
            {
                return;
            }

            foreach (var attribute in attributes)
            {
                if (attribute.IsSupportsDeferredBindingAttribute() && !symbol.IsInputOrTriggerBinding())
                {
                    var location = Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span);
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DeferredBindingAttributeNotSupported, location, attribute.AttributeClass.Name);
                    symbolAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
