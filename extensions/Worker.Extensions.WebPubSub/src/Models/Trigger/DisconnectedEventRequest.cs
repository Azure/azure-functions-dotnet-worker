// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Disconnected event request.
    /// </summary>
    public sealed class DisconnectedEventRequest : WebPubSubEventRequest
    {
        /// <summary>
        /// Reason of the disconnect event.
        /// </summary>
        public string Reason { get; set; }
    }
}
