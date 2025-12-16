// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using IOPath = System.IO.Path;

namespace Azure.Functions.Sdk.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = IOPath.Combine(IOPath.GetTempPath(), IOPath.GetRandomFileName());
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetRandomCsproj()
    {
        // This will become RootNamespace. Ensure it is a valid root namespace. GetRandomFileName()
        // can return invalid namespaces.
        string name = "azfunc.test_" + IOPath.GetRandomFileName().Replace('.', '_') + ".csproj";
        return IOPath.Combine(Path, name);
    }

    public string GetRandomFile(string? ext = null)
    {
        string path = IOPath.Combine(Path, IOPath.GetRandomFileName());
        if (!string.IsNullOrEmpty(ext))
        {
            if (!ext.StartsWith('.'))
            {
                ext = "." + ext;
            }

            path += ext;
        }

        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore exceptions during cleanup
        }
    }
}
