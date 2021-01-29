using System;
using Microsoft.Azure.Functions.Worker.Context;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestFunctionInvocation : FunctionInvocation
    {
        public TestFunctionInvocation()
        {
            InvocationId = Guid.NewGuid().ToString();
            FunctionId = Guid.NewGuid().ToString();
        }

        public override IValueProvider ValueProvider { get; set; }

        public override string InvocationId { get; set; }

        public override string FunctionId { get; set; }

        public override string TraceParent { get; set; }

        public override string TraceState { get; set; }
    }
}
