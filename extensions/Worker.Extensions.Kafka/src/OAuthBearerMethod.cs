// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines the OAuth bearer method
    /// </summary>
    public enum OAuthBearerMethod
    {
        Default,
        Oidc,

        /// <summary>
        /// OIDC client-credentials flow performed in managed .NET code rather than
        /// delegated to librdkafka's libcurl-based token fetch. Requires a host
        /// extension (Microsoft.Azure.WebJobs.Extensions.Kafka) that supports this
        /// mode; avoids the platform-specific CA-bundle issue that affects
        /// librdkafka's OIDC path on some Linux images (e.g. Azure Functions Flex).
        /// </summary>
        OidcManaged
    }
}
