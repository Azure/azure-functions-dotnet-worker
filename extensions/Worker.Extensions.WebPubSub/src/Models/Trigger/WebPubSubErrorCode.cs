// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Response Error Code.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WebPubSubErrorCode
    {
        /// <summary>
        /// Unauthorized error.
        /// </summary>
        Unauthorized,
        /// <summary>
        /// User error.
        /// </summary>
        UserError,
        /// <summary>
        /// Server error.
        /// </summary>
        ServerError
    }
}
