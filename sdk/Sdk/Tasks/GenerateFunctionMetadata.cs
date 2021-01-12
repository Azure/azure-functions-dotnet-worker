using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    public class GenerateFunctionMetadata : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            var generator = new FunctionMetadataGenerator();
            var functions = generator.GenerateFunctionMetadata(AssemblyPath);

            FunctionMetadataJsonWriter.WriteMetadata(functions, OutputPath);

            return true;
        }
    }
}
