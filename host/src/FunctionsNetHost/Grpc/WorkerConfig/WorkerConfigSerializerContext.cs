// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace FunctionsNetHost.Grpc
{
    [JsonSerializable(typeof(WorkerConfig))]
    [JsonSourceGenerationOptions(IncludeFields = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class WorkerConfigSerializerContext : JsonSerializerContext { }
}
