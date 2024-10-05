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
    private const string ConfigurationBuilderFullName = "Microsoft.Extensions.Configuration.IConfigurationBuilder";
    private const string LocalSettingsJsonFileName = "local.settings.json";
    
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
        
        var configBuilderType = context.SemanticModel.Compilation.GetTypeByMetadataName(ConfigurationBuilderFullName);
        
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReceiverType, configBuilderType))
        {
            return;
        }

        // easiest case - local.settings.json is a string literal
        var firstArgument = arguments.First().Expression;
        if (firstArgument is LiteralExpressionSyntax { Token.ValueText: LocalSettingsJsonFileName })
        {
            ReportDiagnostic(context, firstArgument);
            return;
        }
        
        // it can also be a constant value
        var constantValue = context.SemanticModel.GetConstantValue(firstArgument);
        if (constantValue is { HasValue: true, Value: LocalSettingsJsonFileName })
        {
            ReportDiagnostic(context, firstArgument);
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

        if (!dataFlow.DataFlowsIn.Any(s => SymbolEqualityComparer.Default.Equals(s, symbol)))
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
                        Token.ValueText: LocalSettingsJsonFileName
                    }
                })
            {
                ReportDiagnostic(context, firstArgument);
                return;
            }
        }
    }
    
    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        var diagnostic = CreateDiagnostic(expression);
        context.ReportDiagnostic(diagnostic);
    }

    private static Diagnostic CreateDiagnostic(ExpressionSyntax expression)
    {
        return Diagnostic.Create(
            DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
            expression.GetLocation());
    }
}
