// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to send message to a group.
    /// </summary>
    public class SendToGroupAction : WebPubSubAction
    {
        /// <summary>
        /// Target group name.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Message to send.
        /// </summary>
        public BinaryData Data { get; set; }

        /// <summary>
        /// Message data type.
        /// </summary>
        public WebPubSubDataType DataType { get; set; } = WebPubSubDataType.Text;

        /// <summary>
        /// ConnectionIds to exclude.
        /// </summary>
        public IList<string> Excluded { get; set; } = new List<string>();
    }
}
