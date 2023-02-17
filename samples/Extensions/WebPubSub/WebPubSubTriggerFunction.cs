// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp;

public class WebPubSubTriggerFunction
{
    // TODO: write a sample to return both output and trigger response.

    //[Function("Broadcast")]
    //[WebPubSubOutput(Hub = "chat")]
    //public static async Task<UserEventResponse> Run(
    //[WebPubSubTrigger("chat", WebPubSubEventType.User, "message")] UserEventRequest request,
    //BinaryData data,
    //WebPubSubDataType dataType)
    //{
    //    await actions.AddAsync(WebPubSubAction.CreateSendToAllAction(
    //        BinaryData.FromString($"[{request.ConnectionContext.UserId}] {data.ToString()}"),
    //        dataType));
    //    return new UserEventResponse
    //    {
    //        Data = BinaryData.FromString("[SYSTEM] ack"),
    //        DataType = WebPubSubDataType.Text
    //    };
    //}
}
