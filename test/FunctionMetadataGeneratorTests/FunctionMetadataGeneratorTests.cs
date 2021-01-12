using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk;
using Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader;
using Microsoft.Extensions.Options;
using Xunit;

namespace FunctionMetadataGeneratorTests
{
    public class FunctionMetadataGeneratorTests
    {
        [Fact]
        public async Task Json()
        {
            // Currently a dummy test for local validation of serializing/deserializing
            var generator = new FunctionMetadataGenerator();
            var functions = generator.GenerateFunctionMetadata(@"C:\git\dotnet_5\azure-functions-dotnet-worker\samples\FunctionApp\bin\Debug\net5.0\FunctionApp.dll");

            FunctionMetadataJsonWriter.WriteMetadata(functions, string.Empty);

            var abc = new FunctionMetadataJsonReader(new OptionsWrapper<FunctionMetadataJsonReaderOptions>(new FunctionMetadataJsonReaderOptions()));

            var functions2 = await abc.ReadMetadataAsync();
        }
    }
}
