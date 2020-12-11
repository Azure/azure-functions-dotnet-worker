using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.FunctionProvider.SourceBuilder
{
    internal sealed class FunctionMetadataSourceBuilder : ISourceBuilder
    {
        private readonly Compilation _compilation;
        private readonly MethodDeclarationSyntax _methodSyntax;
        private readonly SemanticModel _semanticModel;
        private readonly ISymbol _methodSymbol;
        private readonly string _functionName;

        private StringBuilder? _sourceStringBuilder;

        public FunctionMetadataSourceBuilder(Compilation compilation, MethodDeclarationSyntax methodSyntax, string functionName)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _methodSyntax = methodSyntax ?? throw new ArgumentNullException(nameof(methodSyntax));
            _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));

            _semanticModel = _compilation.GetSemanticModel(_methodSyntax.SyntaxTree);
            _methodSymbol = _semanticModel.GetDeclaredSymbol(_methodSyntax) ?? throw new InvalidOperationException("Cannot find method symbol from method syntax.");
        }

        public string Build()
        {
            _sourceStringBuilder = new StringBuilder();

            AddBasicFunctionMetadata();
            AddBindingsFromParameters();

            return _sourceStringBuilder.ToString();
        }

        private void AddBasicFunctionMetadata()
        {
            var assemblyName = _compilation.Assembly.Name;
            var functionClass = _methodSymbol.ContainingType;
            var scriptFile = $"bin/{assemblyName}.dll";
            var entryPoint = $"{functionClass}.{_methodSyntax.Identifier.ValueText}";

            _sourceStringBuilder!.Append(@$"
                var {_functionName} = new FunctionMetadata
                {{
                    Name = ""{_functionName}"",
                    ScriptFile = ""{scriptFile}"",
                    EntryPoint = ""{entryPoint}"",
                    Language = ""{WorkerConstants.WorkerName}"",
                }};
                {_functionName}.Properties[""IsCodeless""] = false;
            ");
        }

        private void AddBindingsFromParameters()
        {
            foreach (ParameterSyntax parameter in _methodSyntax.ParameterList.Parameters)
            {
                if (parameter.AttributeLists.Count == 0)
                {
                    continue;
                }

                BindingMetadataSourceBuilder bindingBuilder = new(parameter, _semanticModel, _functionName);
                _sourceStringBuilder!.AppendLine(bindingBuilder.Build());
            }
        }
    }
}
