using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LocalSettingsJsonNotAllowedAsConfiguration : DiagnosticAnalyzer
{
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
        // check if the method is AddJsonFile() and it has at least 1 argument
        if (context.Node is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "AddJsonFile" },
                ArgumentList.Arguments: { Count: > 0 } arguments
            } invocation)
        {
            return;
        }
        
        // safeguard to be sure that the target of invocation is IConfigurationBuilder
        if (context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }
        
        var configBuilderType = context.SemanticModel.Compilation.GetTypeByMetadataName(
            "Microsoft.Extensions.Configuration.IConfigurationBuilder");
        
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReceiverType, configBuilderType))
        {
            return;
        }

        // easiest case - local.settings.json is a string literal
        var firstArgument = arguments.First().Expression;
        if (firstArgument is LiteralExpressionSyntax { Token.ValueText: "local.settings.json" } literal)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                literal.GetLocation());

            context.ReportDiagnostic(diagnostic);
            return;
        }
        
        // it can also be a constant value
        var constantValue = context.SemanticModel.GetConstantValue(firstArgument);
        if (constantValue is { HasValue: true, Value: "local.settings.json" })
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                firstArgument.GetLocation());

            context.ReportDiagnostic(diagnostic);
            return;
        }
        
        // we can try to resolve variable passed as well
        // Use DataFlowAnalysis to resolve the value of the variable if possible
        if (firstArgument is not IdentifierNameSyntax identifier)
        {
            return;
        }

        var dataFlow = context.SemanticModel.AnalyzeDataFlow(invocation);
        if (dataFlow?.Succeeded != true)
        {
            return;
        }
        
        // Check for variable assignments within the data flow
        if (context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ILocalSymbol symbol)
        {
            return;
        }

        var variableDeclaration = dataFlow.DataFlowsIn.FirstOrDefault(s => SymbolEqualityComparer.Default.Equals(s, symbol));
        if (variableDeclaration is null)
        {
            return;
        }
        
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(context.CancellationToken);
            if (syntax is VariableDeclaratorSyntax
                {
                    Initializer.Value: LiteralExpressionSyntax
                    {
                        Token.ValueText: "local.settings.json"
                    }
                })
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                    firstArgument.GetLocation());

                context.ReportDiagnostic(diagnostic);
                
                return;
            }
        }
    }
}
