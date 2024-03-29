﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Service Bus entity type.
    /// </summary>
    public enum ServiceBusEntityType
    {
        /// <summary>
        /// Service Bus Queue
        /// </summary>
        Queue,

        /// <summary>
        /// Service Bus Topic
        /// </summary>
        Topic
    }
}
