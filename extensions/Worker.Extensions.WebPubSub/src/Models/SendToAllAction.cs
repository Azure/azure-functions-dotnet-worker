// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to send message to all.
    /// </summary>
    public sealed class SendToAllAction : WebPubSubAction
    {
        /// <summary>
        /// Message to broadcast.
        /// </summary>
        public BinaryData Data { get; set; }

        /// <summary>
        /// Message data type.
        /// </summary>
        public WebPubSubDataType DataType { get; set; } = WebPubSubDataType.Text;

        /// <summary>
        /// ConnectionIds to excluded.
        /// </summary>
        public IList<string> Excluded { get; set; } = new List<string>();
    }
}
