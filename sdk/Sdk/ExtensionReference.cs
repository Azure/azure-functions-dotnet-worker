// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    public class ExtensionReference
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("typeName")]
        public string? TypeName { get; set; }

        [JsonPropertyName("hintPath")]
        public string? HintPath { get; set; }
    }
}
