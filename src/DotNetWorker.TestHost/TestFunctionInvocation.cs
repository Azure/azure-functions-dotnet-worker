using System;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestFunctionInvocation : FunctionInvocation
    {
        public TestFunctionInvocation(string functionId)
        {
            FunctionId = functionId ?? throw new ArgumentNullException(nameof(functionId));

            TraceContext = new TestTraceContext();
        }

        public override string Id { get; } = Guid.NewGuid().ToString();

        public override string FunctionId { get; }

        public override TraceContext TraceContext { get; }
    }
}
