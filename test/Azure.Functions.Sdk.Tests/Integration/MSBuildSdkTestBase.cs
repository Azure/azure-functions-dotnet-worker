// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AwesomeAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public abstract class MSBuildSdkTestBase : MSBuildTestBase, IDisposable
{
    protected MSBuildSdkTestBase()
    {
        TestRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestRootPath);
        File.WriteAllText(
            Path.Combine(TestRootPath, "nuget.config"),
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
            <packageSources>
                <clear />
                <add key="nuget" value="https://api.nuget.org/v3/index.json" />
            </packageSources>
            </configuration>
            """);
    }

    protected string TestRootPath { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal static void MatchMessage(
        BuildErrorEventArgs error, LogMessage log, params string[] args)
    {
        error.Code.Should().Be(log.Code);
        error.Message.Should().Be(log.Format(args));
        error.HelpKeyword.Should().Be(log.HelpKeyword);
    }

    protected DirectoryInfo CreateFiles(string directoryName, params string[] files)
    {
        DirectoryInfo directory = new(Path.Combine(TestRootPath, directoryName));

        foreach (FileInfo file in files.Select(i => new FileInfo(Path.Combine(directory.FullName, i))))
        {
            file.Directory?.Create();
            File.WriteAllText(file.FullName, file.FullName.Substring(directory.FullName.Length + 1));
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
}
