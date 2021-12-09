using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal interface IInputDataSetup
    {
        IDictionary<string, object?> SetupInputData(FunctionContext context);
    }

    public interface ITriggerMetadataSetup
    {
        IDictionary<string, object?> SetupTriggerMetadata(FunctionContext context);
    }
}
