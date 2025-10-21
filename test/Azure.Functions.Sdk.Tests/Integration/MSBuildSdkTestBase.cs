// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public abstract class MSBuildSdkTestBase : MSBuildTestBase, IDisposable
{
    protected static ImmutableDictionary<string, string> GlobalPropertiesDesignTime =>
        ImmutableDictionary.CreateRange<string, string>(
        [
            new("DesignTimeBuild", "true"),
        ]);

    private readonly TempDirectory _temp = new();

    protected MSBuildSdkTestBase()
    {
        _temp.WriteNugetConfig();
    }

    protected string TestRootPath => _temp.Path;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _temp.Dispose();
        }
    }

    protected string GetTempCsproj() => _temp.GetRandomFile(ext: ".csproj");
}
