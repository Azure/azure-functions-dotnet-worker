using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    public interface IFunctionDefinitionFactory
    {
        FunctionDefinition Create(FunctionLoadRequest request);
    }
}
