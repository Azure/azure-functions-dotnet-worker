// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    internal class InitializationResponse
    {
        public IDictionary<string, string> Capabilities = new Dictionary<string, string>();

        public string? WorkerVersion { get; set; }
    }
}
