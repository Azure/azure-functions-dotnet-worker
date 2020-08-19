using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            using Microsoft.Azure.WebJobs.Extensions.Http;
            using Microsoft.Extensions.DependencyInjection;
            using Newtonsoft.Json;
            using Newtonsoft.Json.Converters;
            using Newtonsoft.Json.Linq;
 
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
                    var metadataList = new List<FunctionMetadata>();
                    var raw = new JObject();
                    var httpMethod = new JArray();");

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
                    var attributeName = attribute.Attributes.First().Name.ToString(); // TODO: see how this is affected when there are multiple attributes, but should only have one trigger at a time anyways
                    var attributeSymbolInfo = model.GetSymbolInfo(attribute.Attributes.First().Name);

                    if (attributeName.Contains("Trigger"))
                    {

                        var functionClass = (ClassDeclarationSyntax)parameter.Parent.Parent.Parent;
                        var functionName = functionClass.Identifier.ValueText;
                        var scriptFile = Path.Combine("../bin/" + assemblyName + ".dll");
                        var functionMethod = (MethodDeclarationSyntax)parameter.Parent.Parent;
                        var entryPoint = assemblyName + "." + functionName + "." + functionMethod.Identifier.ValueText;

                        // create binding metadata w/ info below and add to function metadata created above
                        var triggerName = parameter.Identifier.ValueText; // correct? 
                        var triggerType = attributeName;

                        // create raw JObject for the BindingMetadata
                        sourceBuilder.Append(@"raw = new JObject();
                        raw[""name""] = """ + triggerName + @""";
                        raw[""direction""] = ""in"";
                        raw[""type""] = """ + triggerType + @""";
                        httpMethod = new JArray();");

                        var triggerArguments = attribute.Attributes.First().ArgumentList.Arguments;
                        foreach (var arg in triggerArguments)
                        {
                            if (arg.NameEquals != null)
                            {
                                var argType = arg.NameEquals.ToString().ToLower();
                                argType = argType.Remove(argType.LastIndexOf("="));
                                argType = argType.Trim();
                                var argVal = arg.Expression.ToString().Replace("\"", "");
                                if (argType == "route" && argVal == "null")
                                {
                                    Console.WriteLine("Skipping because route set to null"); // not sure what this does, wasn't put inside function.json from sdk anyways and seems to mess up the host loading
                                }
                                else
                                {
                                    sourceBuilder.Append(@"
                                    raw[""" + argType + @"""] = """ + argVal + @""";
                                    ");
                                }
                            }
                            else
                            {
                                if (triggerType == "HttpTrigger")
                                {
                                    if (arg.Expression.ToString().Contains("Authorization"))
                                    {

                                        sourceBuilder.Append(@"
                                        raw[""authLevel""] = ""anonymous""; "); // TODO: actually check the value later
                                    }
                                    else
                                    {
                                        sourceBuilder.Append(@"
                                        if (raw[""method""] != null)
                                        {
                                            httpMethod.Add(" + arg.Expression.ToString() + @");
                                        }
                                        else
                                        {
                                            raw[""method""] = httpMethod;
                                            httpMethod.Add(" + arg.Expression.ToString() + @");
                                        }
                                        ");
                                    }
                                }
                                else if (triggerType == "QueueTrigger")
                                {
                                    sourceBuilder.Append(@"raw[""queueName""] = " + arg.Expression.ToString() + @";");
                                }
                            }
                        }

                        //Assembly assembly = Assembly.LoadFile("C:\\repos\\azure-functions-dotnet-worker\\samples\\FunctionApp\\bin\\Debug\\net5.0\\Microsoft.Azure.WebJobs.Extensions.Http.dll");
                        //var types = assembly.GetTypes();
                        //var triggerObjectType = assembly.GetType(attributeType.Name.ToString());
                        //var triggerObject = Activator.CreateInstance(triggerObjectType);


                        sourceBuilder.Append(@"
                             var " + functionName + @"= new FunctionMetadata
                             {
                                 Name = """ + functionName);
                        sourceBuilder.Append(@""",
                                ScriptFile = """ + scriptFile);
                        sourceBuilder.Append(@""",
                                 EntryPoint = """ + entryPoint);
                        sourceBuilder.Append(@""",
                                 Language = ""dotnet5"", 
                             };" +
                             functionName + @".Bindings.Add(BindingMetadata.Create(raw));
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
#if DEBUG
            Debugger.Launch();
#endif
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