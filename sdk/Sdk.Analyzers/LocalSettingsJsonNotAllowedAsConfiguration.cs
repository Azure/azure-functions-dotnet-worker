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
        
        // Get the symbol for the method being invoked
        if (context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var configBuilderType = context.SemanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Configuration.IConfigurationBuilder");
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReceiverType, configBuilderType))
        {
            return;
        }

        var firstArgument = arguments.First().Expression;
        if (firstArgument is LiteralExpressionSyntax { Token.ValueText: "local.settings.json" } literal)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                literal.GetLocation());

            context.ReportDiagnostic(diagnostic);
            return;
        }
        
        var constantValue = context.SemanticModel.GetConstantValue(firstArgument);
        if (constantValue is { HasValue: true, Value: "local.settings.json" })
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
                firstArgument.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
