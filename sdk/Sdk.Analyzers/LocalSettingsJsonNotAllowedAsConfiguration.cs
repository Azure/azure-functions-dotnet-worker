using System.Collections.Generic;
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

        // This analyzer handles only the overloads of AddJsonFile() with a string as first argument
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!IsAddJsonFileMethodWithArguments(invocation, context))
            {
                return;
            }

            var firstArgument = invocation.ArgumentList.Arguments.First().Expression;
            if (!IsOfStringType(firstArgument, context))
            {
                return;
            }

            var argumentValue = GetValue(firstArgument, context);
            if (argumentValue is not null && argumentValue.EndsWith(LocalSettingsJsonFileName))
            {
                var diagnostic = CreateDiagnostic(firstArgument);
                context.ReportDiagnostic(diagnostic);
            }
        }
        
        
        private static string GetValue(ExpressionSyntax argument, SyntaxNodeAnalysisContext context)
        {
            if (argument is LiteralExpressionSyntax literal)
            {
                return literal.Token.ValueText;
            }
            
            var constantValue = context.SemanticModel.GetConstantValue(argument);
            if (constantValue.HasValue)
            {
                return constantValue.Value.ToString();
            }

            if (argument is not IdentifierNameSyntax identifier)
            {
                return null;
            }

            var innerDataFlow = context.SemanticModel.AnalyzeDataFlow(context.Node);
            if (!innerDataFlow.Succeeded)
            {
                return null;
            }
            
            var identifierSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;

            var finder = new AssignmentFinder(context.SemanticModel, identifierSymbol);
            var syntaxTree = (CSharpSyntaxNode)context.Node.SyntaxTree.GetRoot();
            syntaxTree.Accept(finder);
            var assignments = finder.GetAssignments();

            // todo - cleanup
            if (assignments.Any())
            {
                var l = (LiteralExpressionSyntax)assignments.Last().Right;
                return l.Token.ValueText;
            }
            
            return innerDataFlow.DataFlowsIn
                .Where(symbol => SymbolEqualityComparer.Default.Equals(symbol, identifierSymbol))
                .SelectMany(symbol => symbol.DeclaringSyntaxReferences)
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .Select(variable => variable.Initializer?.Value)
                .OfType<LiteralExpressionSyntax>()
                .LastOrDefault()?.Token.ValueText;
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
        
        public class AssignmentFinder : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly ISymbol _symbolToFind;
            private readonly List<AssignmentExpressionSyntax> _assignments = new();

            public AssignmentFinder(SemanticModel semanticModel, ISymbol symbolToFind)
            {
                _semanticModel = semanticModel;
                _symbolToFind = symbolToFind;
            }

            public IReadOnlyList<AssignmentExpressionSyntax> GetAssignments()
            {
                return _assignments;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                base.VisitAssignmentExpression(node);

                // Check if the left-hand side matches the symbol we are looking for
                var leftSymbol = _semanticModel.GetSymbolInfo(node.Left).Symbol;

                if (leftSymbol != null && SymbolEqualityComparer.Default.Equals(leftSymbol, _symbolToFind))
                {
                    _assignments.Add(node);
                }
            }
        }
    }
    
    
}
