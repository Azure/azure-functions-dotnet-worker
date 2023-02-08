// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Message data type.
    /// </summary>
    public enum WebPubSubDataType
    {
        /// <summary>
        /// binary of content type application/octet-stream.
        /// </summary>
        Binary,
        /// <summary>
        /// json of content type application/json.
        /// </summary>
        Json,
        /// <summary>
        /// text of content type text/plain.
        /// </summary>
        Text
    }
}
