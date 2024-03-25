// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionLib01
{
    public class HttpFunction
    {
        [Function("FunctionLib01_Hello")]
        public HttpResponseData Hello([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
