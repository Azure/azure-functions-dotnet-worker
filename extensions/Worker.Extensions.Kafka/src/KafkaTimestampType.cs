// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines the type of a Kafka record timestamp.
    /// </summary>
    public enum KafkaTimestampType
    {
        /// <summary>Timestamp type is not available.</summary>
        NotAvailable = 0,

        /// <summary>Timestamp was set by the producer (record creation time).</summary>
        CreateTime = 1,

        /// <summary>Timestamp was set by the broker (log append time).</summary>
        LogAppendTime = 2,
    }
}
