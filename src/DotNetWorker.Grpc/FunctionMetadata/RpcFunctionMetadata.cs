using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class RpcFunctionMetadata : IFunctionMetadata
    {
        IList<string> IFunctionMetadata.RawBindings => RawBindings;
    }
}
