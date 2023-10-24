// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    /// <summary>
    /// Analyzer to verify whether expected registration is present for ASP.NET Core Integration.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegistrationExpectedInASPNetIntegration : DiagnosticAnalyzer
    {
        /// Diagnostics supported by the analyzer
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration);

        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";
        private const string ExpectedRegistrationMethod2 = "ConfigureFunctionsWorkerDefaults";

        /// Initialization method
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var symbol = (IMethodSymbol)context.Symbol;

            if (!IsMainMethod(symbol))
            {
                return;
            }

            var syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            var root = syntaxReference?.SyntaxTree.GetRoot();

            var methodCallExpressions = root?.DescendantNodes().OfType<InvocationExpressionSyntax>();

            var correctMethodCallExpressions = methodCallExpressions?.Where(invocation => (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText == ExpectedRegistrationMethod);
            var methodInvocationPresent = correctMethodCallExpressions?.Any();

            var incorrectMethodCallExpressions = methodCallExpressions?.Where(invocation => (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText == ExpectedRegistrationMethod2);
            var methodInvocationPresent2 = incorrectMethodCallExpressions?.Any();


            var a  = incorrectMethodCallExpressions.FirstOrDefault();
            

            if (methodInvocationPresent != null && (bool)methodInvocationPresent)
            {
                return;
            }

            if ((bool)methodInvocationPresent2 && !(bool)methodInvocationPresent)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration, a.GetLocation(), ExpectedRegistrationMethod);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Checks if a method symbol is a Main method. This also checks for implicit main in top-level statements
        private static bool IsMainMethod(IMethodSymbol symbol)
        {
            return symbol?.IsStatic == true && symbol.Name switch
            {
                "Main" => true,
                "$Main" => true,
                "<Main>$" => true,
                _ => false
            };
        }
    }
}
