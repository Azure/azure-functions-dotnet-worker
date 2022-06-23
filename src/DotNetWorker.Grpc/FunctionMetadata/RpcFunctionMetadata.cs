using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class RpcFunctionMetadata : IFunctionMetadata
    {
        IEnumerable<string> IFunctionMetadata.RawBindings => RawBindings;
    }
}
