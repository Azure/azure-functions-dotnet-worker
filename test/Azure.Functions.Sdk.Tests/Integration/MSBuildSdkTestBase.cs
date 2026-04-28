// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Evaluation;
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
        // NUGET_SHOW_STACK=true enables full stack traces for NuGet task errors, which helps debugging test failures.
        Environment.SetEnvironmentVariable("NUGET_SHOW_STACK", "true");
        Directory.CreateDirectory(TestRootPath);
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

    // Ensure this starts with a non-numeric character to be a valid csproj name.
    protected string GetTempCsproj() => _temp.GetRandomCsproj();
}
