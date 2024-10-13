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

            foreach (var literal in FindLiterals(firstArgument, context))
            {
                if (Path.GetFileName(literal) == LocalSettingsJsonFileName)
                {
                    var diagnostic = CreateDiagnostic(firstArgument);
                    context.ReportDiagnostic(diagnostic);
                    break;
                }
            }
        }
        
        
        private static IEnumerable<string> FindLiterals(ExpressionSyntax argument, SyntaxNodeAnalysisContext context)
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
            
            var declarationLiteral = identifierSymbol?.DeclaringSyntaxReferences                
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .Select(variable => variable.Initializer?.Value)
                .OfType<LiteralExpressionSyntax>()
                .LastOrDefault();

            if (declarationLiteral is not null)
            {
                yield return declarationLiteral.Token.ValueText;
            }
            
            var root = context.Node.SyntaxTree.GetRoot(context.CancellationToken);
            foreach (var node in root.DescendantNodes())
            {
                if (node is AssignmentExpressionSyntax assignment)
                {
                    var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(leftSymbol, identifierSymbol) 
                        && assignment.Right is LiteralExpressionSyntax assignmentLiteral)
                    {
                        yield return assignmentLiteral.Token.ValueText;
                    }
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
