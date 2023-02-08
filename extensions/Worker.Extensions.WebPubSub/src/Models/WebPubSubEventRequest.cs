// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Web PubSub service request.
    /// </summary>
    public abstract class WebPubSubEventRequest
    {
        /// <summary>
        /// Connection context contains connection metadata following CloudEvents.
        /// </summary>
        public WebPubSubConnectionContext ConnectionContext { get; }
    }
}
