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

            if (FindLiterals(firstArgument, context).Any(IsPathToLocalSettingsJson))
            {
                var diagnostic = CreateDiagnostic(firstArgument);
                context.ReportDiagnostic(diagnostic);
            }
        }
        
        // This method ensures that the analyzer doesn't check more than it should.
        // It looks for the literal values of the argument, from the easiest to hardest to find.
        // Current order: literal -> constant -> declaration -> assignment in containing method.
        private static IEnumerable<string> FindLiterals(ExpressionSyntax argument, SyntaxNodeAnalysisContext context)
        {
            if (argument is LiteralExpressionSyntax literal)
            {
                yield return literal.Token.ValueText;
                yield break; // no need to check further
            }
            
            var constant = context.SemanticModel.GetConstantValue(argument);
            if (constant.HasValue)
            {
                yield return constant.Value.ToString();
                yield break; // no need to check further
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

            // usually there should be only 1 declaration but better safe than sorry
            foreach (var declarationLiteral in declarationLiterals)
            {
                yield return declarationLiteral.Token.ValueText;
            }

            // let's check assignments in the containing method
            var containingMethod = argument.Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();
            
            if (containingMethod is null)
            {
                yield break;
            }

            var assignmentsInContainingMethod = containingMethod.DescendantNodes()
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
        
        private static bool IsPathToLocalSettingsJson(string literal)
        {
            return Path.GetFileName(literal) == LocalSettingsJsonFileName;
        }
        
        private static Diagnostic CreateDiagnostic(ExpressionSyntax expression)
        {
            return Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                expression.GetLocation());
        }
    }
}
