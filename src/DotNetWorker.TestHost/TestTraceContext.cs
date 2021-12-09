namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestTraceContext : TraceContext
    {
        public override string TraceParent { get; }

        public override string TraceState { get; }
    }
}
