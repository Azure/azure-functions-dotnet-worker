// (c).NET Foundation.All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DependentAssemblyWithFunctions
{
    internal class InternalFunction
    {
        [Function(nameof(InternalFunction))]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            throw new NotImplementedException();
        }

        [Function("ThisShouldBeSkippedBecauseMethodNotPublic")]
        internal static HttpResponseData Run2([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req)
        {
            throw new NotImplementedException();
        }
    }
}
