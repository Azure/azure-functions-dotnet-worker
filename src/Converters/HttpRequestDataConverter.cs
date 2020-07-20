using System;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace FunctionsDotNetWorker.Converters
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
