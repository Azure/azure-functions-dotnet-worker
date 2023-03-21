using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal class GrpcWorkerConstants
    {
        // Capabilities
        public const string RawHttpBodyBytes = "RawHttpBodyBytes";
        public const string TypedDataCollection = "TypedDataCollection";
        public const string RpcHttpBodyOnly = "RpcHttpBodyOnly";
        public const string RpcHttpTriggerMetadataRemoved = "RpcHttpTriggerMetadataRemoved";
        public const string WorkerStatus = "WorkerStatus";
        public const string UseNullableValueDictionaryForHttp = "UseNullableValueDictionaryForHttp";
        public const string EnableHttpProxying = "EnableHttpProxying";

        // Custom Properties
        public const string HttpProxyPortKey = "HttpProxyPort";
    }
}
