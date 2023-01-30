using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public class FunctionResponse
{
    public FunctionResponse(string invocationId, StatusResult result, TypedData returnValue, IReadOnlyCollection<ParameterBinding> outputData)
    {
        InvocationId = invocationId;
        Result = result;
        ReturnValue = returnValue;
        OutputData = outputData;
    }
    public string InvocationId { get; }
    public StatusResult Result { get; }
    public TypedData ReturnValue { get; }
    public IReadOnlyCollection<ParameterBinding> OutputData { get; }

    internal static FunctionResponse From(InvocationResponse invocationResponse)
    {
        return new FunctionResponse(invocationResponse.InvocationId, StatusResult.From(invocationResponse.Result), TypedData.From(invocationResponse.ReturnValue), invocationResponse.OutputData.Select(ParameterBinding.From).ToArray());
    }
}
