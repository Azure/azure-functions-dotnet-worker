namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestBindingMetadata : BindingMetadata
    {
        public TestBindingMetadata(WebJobs.Script.Description.BindingMetadata metadata)
        {
            Type = metadata.Type;
            Direction = metadata.Direction == WebJobs.Script.Description.BindingDirection.In ? BindingDirection.In : BindingDirection.Out;
        }

        public override string Type { get; }

        public override BindingDirection Direction { get; }
    }
}
