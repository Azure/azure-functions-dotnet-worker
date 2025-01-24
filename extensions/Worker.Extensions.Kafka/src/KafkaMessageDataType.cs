// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines the message type for key or value as enum.
    /// </summary>
    public enum KafkaMessageKeyDataType
    {
        Int = 0,
        Long,
        String,
        Float,
        Double,
        Binary
    }
}
