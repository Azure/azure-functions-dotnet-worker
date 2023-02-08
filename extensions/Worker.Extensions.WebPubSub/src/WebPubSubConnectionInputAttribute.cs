// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides <see cref="WebPubSubConnection"/> for a Web PubSub client to connect to Web PubSub Service.
    /// </summary>
    public sealed class WebPubSubConnectionInputAttribute : InputBindingAttribute
    {
        /// <summary>
        /// Target Web PubSub service connection string.
        /// </summary>
        public string Connection { get; set; }

        /// <summary>
        /// Target hub name.
        /// </summary>
        public string Hub { get; set; }

        /// <summary>
        /// Client userId.
        /// </summary>
        public string UserId { get; set; }
    }
}
