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
            if (!IsAddJsonFileInvocation(invocation, context.SemanticModel))
            {
                return;
            }

            var firstArgument = invocation.ArgumentList.Arguments.First().Expression;
            if (context.SemanticModel.GetTypeInfo(firstArgument).Type?.SpecialType != SpecialType.System_String)
            {
                // for now this analyzer handles only the straightforward overloads of AddJsonFile()
                return;
            }

            if (IsLocalSettingsJsonLiteral(firstArgument) ||
                IsLocalSettingsJsonConstant(firstArgument, context.SemanticModel) ||
                IsLocalSettingsJsonVariable(firstArgument, invocation, context))
            {
                var diagnostic = CreateDiagnosticWithLocation(firstArgument);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsAddJsonFileInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            return invocation.Expression 
                       is MemberAccessExpressionSyntax { Name.Identifier.Text: AddJsonFileMethodName } memberAccess 
                   && semanticModel.GetSymbolInfo(memberAccess).Symbol is IMethodSymbol methodSymbol 
                   && methodSymbol.ReceiverType?.ToDisplayString() == ConfigurationBuilderFullName;
        }

        private static bool IsLocalSettingsJsonLiteral(ExpressionSyntax argument)
        {
            return argument is LiteralExpressionSyntax literal 
                   && literal.Token.ValueText.EndsWith(LocalSettingsJsonFileName);
        }

        private static bool IsLocalSettingsJsonConstant(ExpressionSyntax argument, SemanticModel semanticModel)
        {
            var constantValue = semanticModel.GetConstantValue(argument);
            return constantValue.HasValue && constantValue.Value.ToString().EndsWith(LocalSettingsJsonFileName);
        }

        private static bool IsLocalSettingsJsonVariable(ExpressionSyntax argument, InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
        {
            if (argument is not IdentifierNameSyntax identifier)
            {
                return false;
            }

            var dataFlow = context.SemanticModel.AnalyzeDataFlow(invocation);
            if (dataFlow is null || !dataFlow.Succeeded)
            {
                return false;
            }

            var identifierSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            
            return dataFlow.DataFlowsIn
                .Where(symbol => SymbolEqualityComparer.Default.Equals(symbol, identifierSymbol))
                .SelectMany(symbol => symbol.DeclaringSyntaxReferences)
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .Select(variable => variable.Initializer?.Value)
                .OfType<LiteralExpressionSyntax>()
                .Any(literal => literal.Token.ValueText.EndsWith(LocalSettingsJsonFileName));
        }
        
        private static Diagnostic CreateDiagnosticWithLocation(ExpressionSyntax expression)
        {
            return Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                expression.GetLocation());
        }
    }
}
