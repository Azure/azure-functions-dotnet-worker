using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator
{
    [Generator]
    public class FunctionGenerator : ISourceGenerator
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
                internal class FunctionGenerator: IFunctionProvider
                {
                    private List<FunctionMetadata> _metadataList;
                    public FunctionGenerator(FunctionMetadata metadata)
                    {
                        _metadataList = new List<FunctionMetadata>();
                        _metadataList.Add(metadata);
                    }
                    public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }
                    public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
                    {
                        return Task.FromResult(_metadataList.ToImmutableArray());
                    }
                }
             }");


            // inject the created source into the users compilation
            context.AddSource("functionGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(InitializationContext context)
        {
            // No initialization required for this one
        }
    }

}
