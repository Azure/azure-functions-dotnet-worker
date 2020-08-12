using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Mono.Cecil;

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
            namespace FunctionProviderGenerator
             {
                public class DefaultFunctionProvider : IFunctionProvider
                {
                    public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }

                    public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
                    {
                        var metadataList = new List<FunctionMetadata>();");

            //var compilation = context.Compilation;
            //var assembly = compilation.Assembly;
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var assemblyRoot = Path.GetDirectoryName(assemblyPath);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(assemblyRoot);

            var readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver
            };

            var module = ModuleDefinition.ReadModule(assemblyPath, readerParams);

            IEnumerable<TypeDefinition> types = module.Types;

            foreach (TypeDefinition type in types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasFunctionNameAttribute())
                    {
                        if (method.HasUnsuportedAttributes(out string error))
                        {
                            //_logger.LogError(error);
                            //yield return null;
                        }
                        else if (method.IsWebJobsSdkMethod())
                        {
                            var functionName = method.GetSdkFunctionName();
                            sourceBuilder.Append(@" 
                                var metadata = new FunctionMetadata
                                {
                                    Name = """);
                            sourceBuilder.Append(functionName);
                            sourceBuilder.Append(@" "",
                                    ScriptFile = ""this dll"",
                                    EntryPoint = ""the method"",
                                    Language = ""dotnet5""
                                }; ");
                        }

                    }
                }
            }

            sourceBuilder.Append(@"
                            // Add all bindings
                            metadata.Bindings.Add(new BindingMetadata
                        {
                            Name = ""req"",
                            Direction = BindingDirection.In,
                            Type = ""HttpTrigger""
                        });");

            sourceBuilder.Append(@"
                        metadataList.Add(metadata);
                        return Task.FromResult(metadataList.ToImmutableArray());
                    }
                    
                    public void LoadFunctionMetadata()
                    {
                    }
                   }
                 }");


            // inject the created source into the users compilation
            context.AddSource("DefaultFunctionProvider.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(InitializationContext context)
        {
            // No initialization required for this one
        }

    }

}
