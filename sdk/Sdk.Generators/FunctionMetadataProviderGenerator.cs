// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Generates a class that implements IFunctionMetadataProvider and the method GetFunctionsMetadataAsync() which returns a list of IFunctionMetadata. 
    /// This source generator indexes a Function App and explicitly creates a list of DefaultFunctionMetadata (which implements IFunctionMetadata) from the functions defined
    /// in the user's compilation. This allows the worker to index functions at build time, rather than waiting for the process to start.
    /// </summary>
    [Generator]
    public partial class FunctionMetadataProviderGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver || receiver.CandidateMethods.Count == 0)
            {
                return;
            }

            var p = new Parser(context);
            IReadOnlyList<GeneratorFunctionMetadata> functionMetadataInfo = p.GetFunctionMetadataInfo(receiver.CandidateMethods);

            if (functionMetadataInfo.Count > 0)
            {
                var e = new Emitter();
                string result = e.Emit(functionMetadataInfo);

                context.AddSource($"GeneratedFunctionMetadataProvider.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        /// <summary>
        /// Register a factory that can create our custom syntax receiver
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
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
                    if (methodSyntax.AttributeLists.Count > 0) // collect all methods with attributes - we will verify they are functions when we have access to symbols to get the full name
                    {
                        CandidateMethods.Add(methodSyntax);
                    }
                }
            }
        }
    }
}
