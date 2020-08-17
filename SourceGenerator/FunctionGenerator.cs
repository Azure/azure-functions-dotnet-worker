using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator
{
    [Generator]
    public class FunctionProvider : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // begin creating the source we'll inject into the users compilation
            var sourceBuilder = new StringBuilder(@"
            using Microsoft.Azure.WebJobs.Script.Description;
            using System.Collections.Immutable;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using FunctionProviderGenerator;
            using Microsoft.Azure.WebJobs;
            using Microsoft.Azure.WebJobs.Hosting;
            using Microsoft.Extensions.DependencyInjection;
 
            [assembly: WebJobsStartup(typeof(Startup))]
 
            namespace FunctionProviderGenerator
             {
                public class Startup : IWebJobsStartup
                {
                    public void Configure(IWebJobsBuilder builder)
                    {           
                        builder.Services.AddSingleton<IFunctionProvider, DefaultFunctionProvider>();
                    }
                }");

            sourceBuilder.Append(@"
             public class DefaultFunctionProvider : IFunctionProvider
            {
                public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }

                public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
                {
                    var metadataList = new List<FunctionMetadata>();");

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            Compilation compilation = context.Compilation;

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (ParameterSyntax parameter in receiver.CandidateParameters)
            {
                SemanticModel model = compilation.GetSemanticModel(parameter.SyntaxTree);
                var assemblyName = compilation.Assembly.Name;

                foreach (AttributeListSyntax attribute in parameter.AttributeLists)
                {
                    var attributeName = attribute.Attributes.First().Name.ToString(); // idk if this works how many attributes are gonna be on a parameter

                    if (attributeName.Contains("Trigger"))
                    {
                        // get functionName
                        var functionClass = (ClassDeclarationSyntax)parameter.Parent.Parent.Parent;
                        var functionName = functionClass.Identifier.ValueText;
                        // get ScriptFile
                        var scriptFile = Path.Combine(parameter.SyntaxTree.FilePath).Replace(@"\", @"\\");
                        //var scriptFile = "../bin/FunctionApp.dll";
                        // get entry point (method name i think)
                        var functionMethod = (MethodDeclarationSyntax)parameter.Parent.Parent;
                        var entryPoint = assemblyName + "." + functionName + "." + functionMethod.Identifier.ValueText;

                        // create binding metadata w/ info below and add to function metadata created above
                        var triggerName = parameter.Identifier.ValueText; // correct? 
                        var triggerType = attributeName;

                        sourceBuilder.Append(@"
                             var " + functionName + @"= new FunctionMetadata
                             {
                                 Name = """ + functionName);
                        sourceBuilder.Append(@""",
                                ScriptFile = """ + scriptFile);
                        sourceBuilder.Append(@""",
                                 EntryPoint = """ + entryPoint);
                        sourceBuilder.Append(@""",
                                 Language = ""dotnet5""
                             };" +
                             functionName + @".Bindings.Add(new BindingMetadata
                             {
                                 Name = """ + triggerName);
                        sourceBuilder.Append(@""",
                                 Direction = BindingDirection.In,
                                 Type = """ + triggerType);
                        sourceBuilder.Append(@"""
                            });
                             metadataList.Add(" + functionName + ");");
                    }
                }
            }



            sourceBuilder.Append(@"
                    return Task.FromResult(metadataList.ToImmutableArray());
                  }
               }
            }");

            // inject the created source into the users compilation
            context.AddSource("DefaultFunctionProvider.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ParameterSyntax> CandidateParameters { get; } = new List<ParameterSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ParameterSyntax parameterSyntax
                    && parameterSyntax.AttributeLists.Count > 0)
                {
                    CandidateParameters.Add(parameterSyntax);
                }
            }
        }
    }

}