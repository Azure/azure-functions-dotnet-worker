using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker.Descriptor
{
    public interface IFunctionDescriptorFactory
    {
        public FunctionDescriptor Create(FunctionLoadRequest request);
    }
}
