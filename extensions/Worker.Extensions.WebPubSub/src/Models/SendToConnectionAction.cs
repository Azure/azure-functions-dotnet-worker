// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to send message to a connection.
    /// </summary>
    public class SendToConnectionAction : WebPubSubAction
    {
        /// <summary>
        /// Target ConnectionId.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Message to send.
        /// </summary>
        public BinaryData Data { get; set; }

        /// <summary>
        /// Message data type.
        /// </summary>
        public WebPubSubDataType DataType { get; set; } = WebPubSubDataType.Text;
    }
}
