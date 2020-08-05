using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionDescriptor
{
    public interface IFunctionDescriptorFactory
    {
        public IFunctionDescriptor Create(FunctionLoadRequest request);
    }
}
