// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    public class ExtensionsMetadata
    {
        [JsonPropertyName("extensions")]
        public IEnumerable<ExtensionReference>? Extensions { get; set; }
    }
}
