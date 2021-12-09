// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines the broker protocols
    /// </summary>
    public enum BrokerProtocol
    {
        // Force that 0 starts like the one from librdkafka
        NotSet = -1,
        Plaintext,
        Ssl,
        SaslPlaintext,
        SaslSsl
    }
}
