// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to close all connections.
    /// </summary>
    public sealed class CloseAllConnectionsAction : WebPubSubAction
    {
        /// <summary>
        /// ConnectionIds to exclude.
        /// </summary>
        public IList<string> Excluded { get; set; } = new List<string>();

        /// <summary>
        /// Reason to close the connections.
        /// </summary>
        public string Reason { get; set; }
    }
}
