using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Worker.Sdk.FunctionProvider.SourceBuilder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.FunctionProvider
{
    [Generator]
    public class FunctionProviderGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Suggested here: https://github.com/dotnet/roslyn/issues/46084
            // Though the underlying issue is fixed, the default severity set there is warning.
            // This way allows us to throw an actual error on any failure.
            try
            {
                ExecuteInternal(context);
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "AFG1001",
                        "An exception was thrown by the Functions generator",
                        "An exception was thrown by the Functions generator: '{0}'",
                        "SourceGenerator",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None,
                    e.ToString()));
            }
        }

        private void ExecuteInternal(GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            FunctionProviderSourceBuilder functionProviderBuilder = new(compilation, receiver.CandidateMethods);

            // inject the created source into the users compilation
            context.AddSource("_GeneratedFunctionProvider.cs", SourceText.From(functionProviderBuilder.Build(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
//#if DEBUG
//            Debugger.Launch();
//#endif
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MethodDeclarationSyntax methodSyntax)
                {
                    foreach (var p in methodSyntax.ParameterList.Parameters)
                    {
                        if (p.AttributeLists.Count > 0)
                        {
                            CandidateMethods.Add(methodSyntax);
                            break;
                        }
                    }
                }
            }
        }
    }
}