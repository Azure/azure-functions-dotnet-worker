// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿namespace Microsoft.Azure.Functions.Worker.Extensions.Kafka
{
    /// <summary>
    /// Defines the broker authentication modes
    /// </summary>
    public enum BrokerAuthenticationMode
    {
        // Force that 0 starts like the one from librdkafka
        NotSet = -1,
        Gssapi,
        Plain,
        ScramSha256,
        ScramSha512
    }
}
