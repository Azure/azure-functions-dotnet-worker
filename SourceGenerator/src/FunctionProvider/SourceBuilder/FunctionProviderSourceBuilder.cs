using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.FunctionProvider.SourceBuilder
{
    internal sealed class FunctionProviderSourceBuilder : ISourceBuilder
    {
        private readonly Compilation _compilation;
        private readonly IList<MethodDeclarationSyntax> _functionCandidates;

        private StringBuilder? _sourceStringBuilder;

        public FunctionProviderSourceBuilder(Compilation compilation, IList<MethodDeclarationSyntax> functionCandidates)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _functionCandidates = functionCandidates ?? throw new ArgumentNullException(nameof(functionCandidates));
        }

        public string Build()
        {
            _sourceStringBuilder = new StringBuilder();

            AddImportsAndNamespace();
            AddStartupClass();
            AddFunctionProvider();

            return _sourceStringBuilder.ToString();
        }

        private void AddImportsAndNamespace()
        {
            _sourceStringBuilder!.Append(@$"
                using Microsoft.Azure.WebJobs.Script.Description;
                using System.Collections.Immutable;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using FunctionProviderGenerator;
                using Microsoft.Azure.WebJobs;
                using Microsoft.Azure.WebJobs.Hosting;
                using Microsoft.Azure.WebJobs.Extensions.Http;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using Newtonsoft.Json;
                using Newtonsoft.Json.Converters;
                using Newtonsoft.Json.Linq;

                [assembly: WebJobsStartup(typeof(_GeneratedStartup))]

                namespace FunctionProviderGenerator
                {{"
            );
        }

        private void AddStartupClass()
        {
            _sourceStringBuilder!.Append(@$"
                internal class _GeneratedStartup : IWebJobsStartup, IWebJobsConfigurationStartup
                {{
                    public void Configure(IWebJobsBuilder builder)
                    {{
                        builder.Services.AddSingleton<IFunctionProvider, _GeneratedFunctionProvider>();
                    }}

                    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
                    {{
                        builder.ConfigurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
                        {{
                            {{ ""languageWorkers:{WorkerConstants.WorkerName}:workerDirectory"", context.ApplicationRootPath }}
                        }});
                    }}
                }}"
            );
        }

        private void AddFunctionProvider()
        {
            _sourceStringBuilder!.Append(@"
            internal class _GeneratedFunctionProvider : IFunctionProvider
            {
                public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }

                public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
                {
                    var metadataList = new List<FunctionMetadata>();
                    var raw = new JObject();"
            );

            foreach (MethodDeclarationSyntax method in _functionCandidates)
            {
                AddFunctionMetadataIfValid(method);
            }

            _sourceStringBuilder!.Append(@"
                    return Task.FromResult(metadataList.ToImmutableArray());
                  }
               }
            }");
        }

        private void AddFunctionMetadataIfValid(MethodDeclarationSyntax method)
        {
            SemanticModel model = _compilation.GetSemanticModel(method.SyntaxTree);
            ISymbol? methodModel = model.GetDeclaredSymbol(method);

            if (methodModel == null)
            {
                return;
            }

            string? functionName = GetFunctionNameOrNull(methodModel);

            // If there isn't a function name, this is not a Function
            if (functionName == null)
            {
                // TODO: This used to return from all candidate methods. Make sure this is right!
                return;
            }

            FunctionMetadataSourceBuilder functionMetadataBuilder = new(_compilation, method, functionName);

            _sourceStringBuilder!.AppendLine(functionMetadataBuilder.Build());
            _sourceStringBuilder.AppendLine($"metadataList.Add({functionName});");
        }

        private string? GetFunctionNameOrNull(ISymbol methodModel)
        {
            string? functionName = null;

            // Find function name on [FunctionName("Function1")]
            foreach (var attr in methodModel.GetAttributes())
            {
                IMethodSymbol? attributeSymbol = attr.AttributeConstructor;
                if (string.Equals(attributeSymbol?.ContainingType.ToString(), "Microsoft.Azure.WebJobs.FunctionNameAttribute"))
                {
                    var props = AttributeDataHelpers.GetAllProperties(attr);
                    functionName = props["name"]?.ToString();

                    // there can only be one
                    break;
                }
            }

            return functionName;
        }
    }
}