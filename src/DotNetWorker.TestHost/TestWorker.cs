using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestWorker : IWorker
    {
        private readonly IFunctionsApplication _application;
        private readonly TestFunctionMap _functionMap;
        private readonly TestFunctionDefinitionFactory _definitionFactory;

        public TestWorker(IFunctionsApplication application, TestFunctionDefinitionFactory definitionFactory, TestFunctionMap functionMap)
        {
            _application = application;
            _functionMap = functionMap;
            _definitionFactory = definitionFactory;
        }

        public async Task StartAsync(CancellationToken token)
        {
            FunctionMetadataJsonReaderOptions options = new()
            {
                FunctionMetadataFileDrectory = ".testproj"
            };

            var reader = new FunctionMetadataJsonReader(new OptionsWrapper<FunctionMetadataJsonReaderOptions>(options));
            ImmutableArray<FunctionMetadata> metadata = await reader.ReadMetadataAsync();

            foreach (var functionMetadata in metadata)
            {
                var definition = _definitionFactory.Create(functionMetadata);
                _functionMap.AddFunction(definition.Name, definition.Id);
                _application.LoadFunction(definition);
            }
        }

        public Task StopAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
