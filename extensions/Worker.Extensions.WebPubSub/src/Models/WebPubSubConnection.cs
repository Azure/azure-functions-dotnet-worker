// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Contains necessary information for a Web PubSub client to connect to Web PubSub Service.
    /// </summary>
    public sealed class WebPubSubConnection
    {
        /// <summary>
        /// Base Uri of the websocket connection.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Uri with accessToken of the websocket connection.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Access token of the websocket connection.
        /// </summary>
        public string AccessToken { get; }
    }
}
