// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp;

internal class WebPubSubContextInputFunction
{
    [Function("connect")]
    public static HttpResponseData Connected(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [WebPubSubContextInput] WebPubSubContext wpsReq)
    {
        var response = req.CreateResponse();
        if (wpsReq.Request is PreflightRequest || wpsReq.ErrorMessage != null)
        {
            response.StatusCode = wpsReq.Response.StatusCode;
            response.WriteString(wpsReq.ErrorMessage);
            return response;
        }
        var request = wpsReq.Request as ConnectEventRequest;

        var wpsResponse = new ConnectEventResponse
        {
            UserId = request.ConnectionContext.UserId
        };
        response.WriteAsJsonAsync(wpsResponse);
        return response;
    }
}
