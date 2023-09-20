// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegistrationExpectedInASPNetIntegration : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration);

        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";
        private const string AspNetExtensionAssemblyName = "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore";

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Namespace);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            bool isAspNetAssembly = context.Compilation.ReferencedAssemblyNames.Where(assembly => assembly.Name.Equals(AspNetExtensionAssemblyName)).Any();
            SyntaxReference syntaxReference = context.Symbol.DeclaringSyntaxReferences.FirstOrDefault();
            string code = syntaxReference.SyntaxTree.GetText().ToString();

            if (isAspNetAssembly && !code.Contains(ExpectedRegistrationMethod))
            {
                var location = Location.Create(syntaxReference.SyntaxTree, syntaxReference.Span);
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration, location, ExpectedRegistrationMethod);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
