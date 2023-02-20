// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Client certificate info.
    /// </summary>
    public sealed class WebPubSubClientCertificate
    {
        /// <summary>
        /// Certificate thumbprint.
        /// </summary>
        public string Thumbprint { get; set; }
    }
}
