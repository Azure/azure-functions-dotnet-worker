// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;

namespace Azure.Functions.Sdk;

internal static class ExpectedFilesHelper
{
    private static readonly string[] _extensions = [".dll"];

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, };

    public static string GetWorkerConfig(string expectedExecutable, string expectedEntryPoint)
    {
        return JsonSerializer.Serialize(
            new
            {
                description = new
                {
                    language = "dotnet-isolated",
                    extensions = _extensions,
                    defaultExecutablePath = expectedExecutable,
                    defaultWorkerPath = expectedEntryPoint,
                    workerIndexing = "true",
                    canUsePlaceholder = true,
                },
            },
            _jsonOptions);
    }
}
