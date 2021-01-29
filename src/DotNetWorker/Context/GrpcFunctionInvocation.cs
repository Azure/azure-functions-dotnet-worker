using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Context
{
    internal class GrpcFunctionInvocation : FunctionInvocation
    {
        public GrpcFunctionInvocation(InvocationRequest invocationRequest)
        {
            InvocationId = invocationRequest.InvocationId;
            FunctionId = invocationRequest.FunctionId;
            TraceParent = invocationRequest.TraceContext.TraceParent;
            TraceState = invocationRequest.TraceContext.TraceState;
        }

        public override string InvocationId { get; set; }

        public override string FunctionId { get; set; }

        public override string TraceParent { get; set; }

        public override string TraceState { get; set; }
    }
}
