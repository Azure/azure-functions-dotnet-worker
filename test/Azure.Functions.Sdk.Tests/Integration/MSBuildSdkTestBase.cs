// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public abstract class MSBuildSdkTestBase : MSBuildTestBase, IDisposable
{
    protected MSBuildSdkTestBase()
    {
        // NUGET_SHOW_STACK=true enables full stack traces for NuGet task errors, which helps debugging test failures.
        Environment.SetEnvironmentVariable("NUGET_SHOW_STACK", "true");
        TestRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestRootPath);
    }

    protected string TestRootPath { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected static ProjectCollection CreateBinaryLoggerCollection()
    {
        string path = Path.Combine(GetArtifactsPath(), $"{CurrentTestAttribute.GetTestName()}.binlog");
        ProjectCollection collection = new();
        collection.RegisterLogger(
            new Microsoft.Build.Logging.BinaryLogger
            {
                Parameters = $"LogFile={path}"
            });

        return collection;
    }

    protected DirectoryInfo CreateFiles(string directoryName, params string[] files)
    {
        DirectoryInfo directory = new(Path.Combine(TestRootPath, directoryName));

        foreach (FileInfo file in files.Select(i => new FileInfo(Path.Combine(directory.FullName, i))))
        {
            file.Directory?.Create();
            File.WriteAllText(file.FullName, Path.GetRelativePath(directory.FullName, file.FullName));
        }

        return directory;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Directory.Exists(TestRootPath))
            {
                try
                {
                    Directory.Delete(TestRootPath, recursive: true);
                }
                catch (Exception)
                {
                    try
                    {
                        Thread.Sleep(500);
                        Directory.Delete(TestRootPath, recursive: true);
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
            }
        }
    }

    protected string GetTempFile(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Path.Combine(TestRootPath, name);
    }

    protected string GetTempFileWithExtension(string? extension = null)
    {
        return Path.Combine(TestRootPath, $"{Path.GetRandomFileName()}{extension ?? string.Empty}");
    }

    protected string GetTempCsproj() => GetTempFileWithExtension(".csproj");

    private static string GetArtifactsPath()
    {
        string root = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") ?? string.Empty;
        return Path.Combine(root, "log", "test");
    }
}
