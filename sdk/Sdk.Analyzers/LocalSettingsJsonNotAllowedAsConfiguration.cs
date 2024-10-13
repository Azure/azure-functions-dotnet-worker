using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalSettingsJsonNotAllowedAsConfiguration : DiagnosticAnalyzer
    {
        private const string ConfigurationBuilderFullName = "Microsoft.Extensions.Configuration.IConfigurationBuilder";
        private const string LocalSettingsJsonFileName = "local.settings.json";
        private const string AddJsonFileMethodName = "AddJsonFile";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        // This analyzer handles only the overloads of AddJsonFile() with a string as first argument
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!IsAddJsonFileMethodWithAtLeastOneArgument(invocation, context))
            {
                return;
            }

            var firstArgument = invocation.ArgumentList.Arguments.First().Expression;
            if (!IsOfStringType(firstArgument, context))
            {
                return;
            }

            if (TraverseLiterals(firstArgument, context).Any(IsLocalSettingsJson))
            {
                var diagnostic = CreateDiagnostic(firstArgument);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsLocalSettingsJson(string literal)
        {
            return Path.GetFileName(literal) == LocalSettingsJsonFileName;
        }

        private static IEnumerable<string> TraverseLiterals(ExpressionSyntax argument, SyntaxNodeAnalysisContext context)
        {
            if (argument is LiteralExpressionSyntax literal)
            {
                yield return literal.Token.ValueText;
            }
            
            var constant = context.SemanticModel.GetConstantValue(argument);
            if (constant.HasValue)
            {
                yield return constant.Value.ToString();
            }

            if (argument is not IdentifierNameSyntax identifier)
            {
                yield break;
            }
            
            var identifierSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (identifierSymbol is null)
            {
                yield break;
            }
            
            var declarationLiterals = identifierSymbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .Select(variable => variable.Initializer?.Value)
                .OfType<LiteralExpressionSyntax>();

            foreach (var declarationLiteral in declarationLiterals)
            {
                yield return declarationLiteral.Token.ValueText;
            }

            // Limit the scope of assignments to check
            var containingMethod = argument.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod == null)
            {
                yield break;
            }

            var assignmentsInContainingMethod = containingMethod
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(assignment => assignment.SpanStart < argument.SpanStart);

            foreach (var assignment in assignmentsInContainingMethod)
            {
                var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
                if (SymbolEqualityComparer.Default.Equals(leftSymbol, identifierSymbol)
                    && assignment.Right is LiteralExpressionSyntax assignmentLiteral)
                {
                    yield return assignmentLiteral.Token.ValueText;
                }
            }
        }

        private static bool IsAddJsonFileMethodWithAtLeastOneArgument(
            InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
        {
            return invocation.ArgumentList.Arguments.Count > 0
                && invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: AddJsonFileMethodName } memberAccess 
                && context.SemanticModel.GetSymbolInfo(memberAccess).Symbol is IMethodSymbol methodSymbol 
                && methodSymbol.ReceiverType?.ToDisplayString() == ConfigurationBuilderFullName;
        }
        
        private static bool IsOfStringType(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            return context.SemanticModel.GetTypeInfo(expression).Type?.SpecialType == SpecialType.System_String;
        }
        
        private static Diagnostic CreateDiagnostic(ExpressionSyntax expression)
        {
            return Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                expression.GetLocation());
        }
    }
}
