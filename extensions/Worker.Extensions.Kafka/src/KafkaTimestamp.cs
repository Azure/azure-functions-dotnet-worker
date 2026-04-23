// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents the timestamp of a Kafka record.
    /// </summary>
    public class KafkaTimestamp
    {
        /// <summary>
        /// Gets or sets the timestamp as Unix milliseconds since epoch.
        /// </summary>
        public long UnixTimestampMs { get; set; }

        /// <summary>
        /// Gets or sets the timestamp type.
        /// </summary>
        public KafkaTimestampType Type { get; set; }

        /// <summary>
        /// Gets the timestamp as a <see cref="System.DateTimeOffset"/>.
        /// </summary>
        public DateTimeOffset DateTimeOffset => System.DateTimeOffset.FromUnixTimeMilliseconds(UnixTimestampMs);
    }
}
