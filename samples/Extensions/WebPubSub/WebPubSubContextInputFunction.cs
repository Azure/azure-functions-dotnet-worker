// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp;

internal class WebPubSubContextInputFunction
{
    // TODO: whether to define the class from Microsoft.Azure.WebPubSub.Common
    [Function("connect")]
    public static void Connect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [WebPubSubContextInput] WebPubSubContext wpsReq)
    {
        //if (wpsReq.Request is PreflightRequest || wpsReq.ErrorMessage != null)
        //{
        //    return wpsReq.Response;
        //}
        //var request = wpsReq.Request as ConnectEventRequest;
        //return request.CreateResponse(request.ConnectionContext.UserId, null, null, null);
    }
}
