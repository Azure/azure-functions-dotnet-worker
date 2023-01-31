// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Parser = FunctionExecutorGenerator.Parser;
using Emitter = FunctionExecutorGenerator.Emitter;
using FunctionMethodSyntaxReceiver = Microsoft.Azure.Functions.Worker.Sdk.Generators.FunctionMetadataProviderGenerator.FunctionMethodSyntaxReceiver;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    [Generator]
    public partial class FunctionExecutorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FunctionMethodSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not FunctionMethodSyntaxReceiver receiver || receiver.CandidateMethods.Count == 0)
            {
                return;
            }

            Parser parser = new Parser(context);
            var funcList = parser.Get(receiver.CandidateMethods);

            var text = new Emitter().Emit(funcList, context.CancellationToken);
            context.AddSource("GeneratedFunctionExecutor.g.cs", SourceText.From(text, Encoding.UTF8));
        }
    }
}
