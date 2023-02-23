// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WebJobsAttributesNotSupported : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(DiagnosticDescriptors.WebJobsAttributesAreNotSupported); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(c =>
            {
                var symbol = (IMethodSymbol)c.Symbol;

                var attributes = symbol.GetAttributes();

                if (attributes.IsEmpty)
                {
                    return;
                }

                if (symbol.IsFunction(c))
                {
                    var webjobsAttributes = symbol.GetWebJobsAttributes();
                    foreach (var attribute in webjobsAttributes)
                    {
                        var location = Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span);
                        c.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.WebJobsAttributesAreNotSupported, location, attribute.AttributeClass.Name));
                    }
                }
            }, SymbolKind.Method);
        }
    }
}
