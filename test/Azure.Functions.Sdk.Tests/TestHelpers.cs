// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;

namespace Azure.Functions.Sdk.Tests;

internal static class TestHelpers
{
    public static string CurrentTestName
    {
        get
        {
            if (CurrentTestAttribute.TryGetTestName(out string? testName))
            {
                return testName!;
            }

            throw new InvalidOperationException("Current test name is not available.");
        }
    }

    public static string ArtifactsPath
    {
        get
        {
            string root = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") ?? string.Empty;
            return Path.Combine(root, "log", "test");
        }
    }

    public static ProjectCollection CreateBinaryLoggerCollection()
    {
        string path = Path.Combine(ArtifactsPath, $"{CurrentTestName}.binlog");
        ProjectCollection collection = new();
        collection.RegisterLogger(
            new Microsoft.Build.Logging.BinaryLogger
            {
                Parameters = $"LogFile={path}"
            });

        return collection;
    }
}
