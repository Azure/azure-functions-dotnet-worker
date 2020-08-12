using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public interface IFunctionDefinitionFactory
    {
        FunctionDefinition Create(FunctionLoadRequest request);
    }
}
