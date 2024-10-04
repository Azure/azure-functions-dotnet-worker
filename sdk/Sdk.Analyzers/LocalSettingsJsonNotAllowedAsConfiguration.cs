using System.Collections.Immutable;
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
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if the method being called is AddJsonFile
        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Name.Identifier.Text: "AddJsonFile"
            })
        {
            return;
        }

        // Check if the first argument is "local.settings.json"
        if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax
            {
                Token.ValueText: "local.settings.json"
            } literal)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.LocalSettingsJsonNotAllowedAsConfiguration,
            literal.GetLocation());

        context.ReportDiagnostic(diagnostic);

    }
}
