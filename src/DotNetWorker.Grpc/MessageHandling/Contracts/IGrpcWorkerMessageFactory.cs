// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;

namespace Microsoft.Azure.Functions.Worker.Grpc;

/// <summary>
/// IGrpcWorkerMessageFactory creates a handler for the given message type. See: <see cref="MsgType"/>
/// </summary>
internal interface IGrpcWorkerMessageFactory
{
    /// <summary>
    /// CreateHandler creates a Grpc worker message handler for the given message type
    /// </summary>
    /// <param name="msgType"><see cref="MsgType"/>Message type</param>
    /// <returns>
    ///     A <see cref="IGrpcWorkerMessageHandler"/> derived handler for the given type.
    ///     Returns null if message type does not have a corresponding handler
    /// </returns>
    IGrpcWorkerMessageHandler? CreateHandler(MsgType msgType);
}
