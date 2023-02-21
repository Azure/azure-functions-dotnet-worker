﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp;

internal class WebPubSubContextInputFunction
{
    [Function("connect")]
    public static HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [WebPubSubContextInput] WebPubSubContext wpsReq)
    {
        var response = req.CreateResponse();
        Console.WriteLine($"Received client connect with connectionId: {wpsReq.Request.ConnectionContext.ConnectionId}");
        if (wpsReq.Request is PreflightRequest || wpsReq.ErrorMessage != null)
        {
            response.WriteAsJsonAsync(wpsReq.Response);
            return response;
        }
        var request = wpsReq.Request as ConnectEventRequest;
        // assign the properties if needed.
        response.WriteAsJsonAsync(request.CreateResponse(request.ConnectionContext.UserId, null, null, null));
        return response;
    }

    // validate method when upstream set as http://<func-host>/api/{event}
    [Function("validate")]
    public static HttpResponseData Validate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options")] HttpRequestData req,
        [WebPubSubContextInput] WebPubSubContext wpsReq)
    {
        return BuildHttpResponseData(req, wpsReq.Response);
    }

    private static HttpResponseData BuildHttpResponseData(HttpRequestData request, SimpleResponse wpsResponse)
    {
        var response = request.CreateResponse();
        response.StatusCode = (HttpStatusCode)wpsResponse.Status;
        response.Body = response.Body;
        foreach (var header in wpsResponse.Headers)
        {
            response.Headers.Add(header.Key, header.Value);
        }
        return response;
    }
}
