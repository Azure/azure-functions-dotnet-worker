// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class BinaryDataExtensions
    {
        public static object? ToObjectFromJson(this BinaryData data, Type type, JsonSerializerOptions? options = null)
        {
            ReadOnlyMemory<byte> bytes = data.ToArray();
            return JsonSerializer.Deserialize(bytes.Span, type, options);
        }
    }
}
