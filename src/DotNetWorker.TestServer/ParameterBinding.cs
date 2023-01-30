using System;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public record ParameterBinding(string BindingName, TypedData Data, DataCase DataCase)
{
    internal static ParameterBinding From(Grpc.Messages.ParameterBinding binding)
    {
        return new ParameterBinding(binding.Name, TypedData.From(binding.Data), Map(binding.RpcDataCase));
    }

    private static DataCase Map(Grpc.Messages.ParameterBinding.RpcDataOneofCase @case) =>
        @case switch
        {
            Grpc.Messages.ParameterBinding.RpcDataOneofCase.None => DataCase.None,
            Grpc.Messages.ParameterBinding.RpcDataOneofCase.Data => DataCase.Data,
            Grpc.Messages.ParameterBinding.RpcDataOneofCase.RpcSharedMemory => DataCase.SharedMemory,
            _ => throw new ArgumentOutOfRangeException(nameof(@case), @case, null)
        };
}
