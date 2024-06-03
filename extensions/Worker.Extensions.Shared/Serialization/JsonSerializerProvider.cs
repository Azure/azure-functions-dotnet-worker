// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
internal static class JsonSerializerOptionsProvider
{
    // Note: If you need an instance with different settings, create a new instance and use that
    // instead of modifying this as it may break other places where this has been used.

    /// <summary>
    /// Option with the below settings:
    ///  1. Deserialization supports case-insensitive properties.
    ///  2. Serialization produces camelCase properties.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
