using System.Text;
using Microsoft.CodeAnalysis;
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
            namespace FunctionProviderGenerator
             {
                public class DefaultFunctionProvider : IFunctionProvider
                {
                    private List<FunctionMetadata> _metadataList;
                    public DefaultFunctionProvider()
                    {
                        _metadataList = new List<FunctionMetadata>();
                    }
                    public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }
                    public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
                    {
                        return Task.FromResult(_metadataList.ToImmutableArray());
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
