// (c).NET Foundation.All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DependentAssemblyWithFunctions
{
    internal class FooInternalClass
    {
        [Function("InternalClassPublicRun")]
        public static HttpResponseData PublicRun([HttpTrigger(AuthorizationLevel.Admin, "get")] HttpRequestData r,
            FunctionContext executionContext)
        {
            throw new NotImplementedException();
        }
    }
}
