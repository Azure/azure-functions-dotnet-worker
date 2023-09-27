// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var symbol = (IMethodSymbol)context.Symbol;

            bool isAspNetAssembly = context.Compilation.ReferencedAssemblyNames.Any(assembly => assembly.Name.Equals(AspNetExtensionAssemblyName));

            if (!isAspNetAssembly || !symbol.IsMainMethod())
            {
                return;
            }

            SyntaxReference syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            var root = syntaxReference.SyntaxTree.GetRoot();

            var methodCallExpressions = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            bool methodInvocationPresent = methodCallExpressions.Any(invocation => (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText == ExpectedRegistrationMethod);

            if (methodInvocationPresent)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration, symbol.Locations.First(), ExpectedRegistrationMethod);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
