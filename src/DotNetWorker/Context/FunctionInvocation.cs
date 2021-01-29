namespace Microsoft.Azure.Functions.Worker.Context
{
    public abstract class FunctionInvocation
    {
        public abstract string InvocationId { get; set; }

        public abstract string FunctionId { get; set; }

        public abstract string TraceParent { get; set; }

        public abstract string TraceState { get; set; }
    }
}
