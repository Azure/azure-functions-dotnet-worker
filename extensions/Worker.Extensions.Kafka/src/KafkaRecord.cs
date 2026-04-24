// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a raw Apache Kafka record with full metadata.
    /// Key and value are raw bytes — the user controls deserialization.
    /// </summary>
    public class KafkaRecord
    {
        /// <summary>
        /// Gets or sets the topic name this record was consumed from.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets the partition this record was consumed from.
        /// </summary>
        public int Partition { get; set; }

        /// <summary>
        /// Gets or sets the offset of this record within the partition.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Gets or sets the raw key bytes. Null if the record has no key.
        /// </summary>
        public byte[] Key { get; set; }

        /// <summary>
        /// Gets or sets the raw value bytes. Null if the record has no value.
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Gets or sets the record timestamp.
        /// </summary>
        public KafkaTimestamp Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the record headers.
        /// </summary>
        public KafkaHeader[] Headers { get; set; }

        /// <summary>
        /// Gets or sets the leader epoch, if available. Null if not provided by the broker.
        /// </summary>
        public int? LeaderEpoch { get; set; }
    }
}
