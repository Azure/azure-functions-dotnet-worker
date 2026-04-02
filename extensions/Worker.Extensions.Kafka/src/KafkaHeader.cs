// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a single Kafka record header (key-value pair where value is raw bytes).
    /// </summary>
    public class KafkaHeader
    {
        /// <summary>
        /// Gets or sets the header key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the header value as raw bytes.
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Gets the header value as a UTF-8 string, or null if the value is null.
        /// </summary>
        public string GetValueAsString()
        {
            return Value is null ? null : Encoding.UTF8.GetString(Value);
        }
    }
}
