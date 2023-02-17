// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp;

public static class WebPubSubOutputFunction
{
    [Function("Notification")]
    [WebPubSubOutput(Hub = "notification")]
    public static WebPubSubAction Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        return new SendToAllAction
        {
            Data = BinaryData.FromString($"[{DateTime.UtcNow}]{Guid.NewGuid()}"),
            DataType = WebPubSubDataType.Text
        };
    }
}
