// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal static class Constants
    {
        // Rpc Constants
        internal const string HttpUriCapability = "HttpUri";

        // Header constants
        internal const string CorrelationHeader = "x-ms-invocation-id";
    }
}
