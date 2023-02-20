// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Connect event request.
    /// </summary>
    public sealed class ConnectEventRequest : WebPubSubEventRequest
    {
        /// <summary>
        /// User Claims.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Claims { get; set; }

        /// <summary>
        /// Request query.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Query { get; set; }

        /// <summary>
        /// Request headers.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Headers { get; set; }

        /// <summary>
        /// Supported subprotocols.
        /// </summary>
        public IReadOnlyList<string> Subprotocols { get; set; }

        /// <summary>
        /// Client certificates.
        /// </summary>
        public IReadOnlyList<WebPubSubClientCertificate> ClientCertificates { get; set; }
    }
}
