// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines data types as enum.
    /// </summary>
    public enum KafkaDataType
    {
        Int = 0,
        Long,
        String,
        Float,
        Double,
        Binary
    }
}
