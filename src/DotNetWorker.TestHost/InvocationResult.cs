using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    public class InvocationResult
    {
        public IDictionary<string, object?> OutputBindings { get; set; }

        public Exception? Exception { get; set; }

        public HttpResponseData? GetResponseData()
        {
            if (OutputBindings.TryGetValue("HttpResponse", out object? response))
            {
                return response as HttpResponseData;
            }

            return null;
        }
    }
}
