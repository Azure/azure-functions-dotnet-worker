namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal class TestFunctionMetadata : FunctionMetadata
    {
        public override string PathToAssembly { get; set; }

        public override string EntryPoint { get; set; }

        public override string FunctionId { get; set; }

        public override string Name { get; set; }
    }
}
