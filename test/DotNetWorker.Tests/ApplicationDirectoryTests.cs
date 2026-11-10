// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Azure.Functions.Tests;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class ApplicationDirectoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void UnconfiguredDirectoryUsesBaseDirectory(string configuredDirectory)
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), "application-base");
        using TestScopedAppContextBaseDirectory application = new(baseDirectory);
        using TestScopedEnvironmentVariable environment = new("FUNCTIONS_APPLICATION_DIRECTORY", configuredDirectory);
        using TestScopedEnvironmentVariable worker = new("FUNCTIONS_WORKER_DIRECTORY", "not-the-application");

        Assert.Equal(baseDirectory, ApplicationDirectory.GetApplicationDirectory());
    }

    [Fact]
    public void ConfiguredDirectoryTakesPrecedenceWithoutProbingOrCaching()
    {
        using TestScopedAppContextBaseDirectory application = new(Path.Combine(Path.GetTempPath(), "application-base"));
        string configuredDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using (TestScopedEnvironmentVariable environment = new("FUNCTIONS_APPLICATION_DIRECTORY", configuredDirectory))
        {
            Assert.False(Directory.Exists(configuredDirectory));
            Assert.Equal(configuredDirectory, ApplicationDirectory.GetApplicationDirectory());
        }

        using TestScopedEnvironmentVariable changed = new("FUNCTIONS_APPLICATION_DIRECTORY", "another-directory");
        Assert.Equal("another-directory", ApplicationDirectory.GetApplicationDirectory());
    }
}
