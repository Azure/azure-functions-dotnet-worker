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
    public class FunctionExecutorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FunctionMethodSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!ShouldExecuteGeneration(context))
            {
                return;
            }

            if (context.SyntaxReceiver is not FunctionMethodSyntaxReceiver receiver || receiver.CandidateMethods.Count == 0)
            {
                return;
            }

            var parser = new Parser(context);
            var functions = parser.GetFunctions(receiver.CandidateMethods);

            if (functions.Count == 0)
            {
                return;
            }
            
            var text = Emitter.Emit(functions, context.CancellationToken);
            context.AddSource(Constants.FileNames.GeneratedFunctionExecutor, SourceText.From(text, Encoding.UTF8));
        }

        private static bool ShouldExecuteGeneration(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                    Constants.BuildProperties.EnablePlaceholder, out var value))
            {
                return false;
            }

            return string.Equals(value, bool.TrueString, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
