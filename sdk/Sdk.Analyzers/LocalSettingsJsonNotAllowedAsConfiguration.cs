using System.Collections.Immutable;
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

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!IsAddJsonFileMethodWithArguments(invocation, context))
            {
                return;
            }

            // This analyzer handles only the overloads of AddJsonFile() with a string as first argument
            var firstArgument = invocation.ArgumentList.Arguments.First().Expression;
            if (!IsOfStringType(firstArgument, context))
            {
                return;
            }

            if (TryGetStringValue(firstArgument, context, out var stringValue) 
                && stringValue.EndsWith(LocalSettingsJsonFileName))
            {
                var diagnostic = CreateDiagnostic(firstArgument);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool TryGetStringValue(ExpressionSyntax argument, SyntaxNodeAnalysisContext context, out string value)
        {
            if (argument is LiteralExpressionSyntax literal)
            {
                value = literal.Token.ValueText;
                return true;
            }
            
            var constantValue = context.SemanticModel.GetConstantValue(argument);
            if (constantValue.HasValue)
            {
                value = constantValue.Value.ToString();
                return true;
            }

            value = null;
            
            if (argument is not IdentifierNameSyntax identifier)
            {
                return false;
            }

            var dataFlow = context.SemanticModel.AnalyzeDataFlow(context.Node);
            if (!dataFlow.Succeeded)
            {
                return false;
            }
            
            var identifierSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            
            value = dataFlow.DataFlowsIn
                .Where(symbol => SymbolEqualityComparer.Default.Equals(symbol, identifierSymbol))
                .SelectMany(symbol => symbol.DeclaringSyntaxReferences)
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .Select(variable => variable.Initializer?.Value)
                .OfType<LiteralExpressionSyntax>()
                .FirstOrDefault()?.Token.ValueText;

            return value is not null;
        }

        private static bool IsAddJsonFileMethodWithArguments(
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
