using System;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker.Converters
{
    public class HttpRequestDataConverter : IParameterConverter
    {
        public bool TryConvert(object source, Type targetType, string name, out object target)
        {
            target = null;

            if (!(source is RpcHttp httpData))
            {
                return false;
            }

            if (targetType != typeof(HttpRequestData))
            {
                return false;
            }

            target = new HttpRequestData(httpData);
            return true;
        }
    }
}
