// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.Factory;

public class AssemblyCompatibilityTests
{
    [Fact]
    public void ValidateReference_AcceptsExactAssemblyVersion()
    {
        var expected = new AssemblyName("Dependency, Version=1.2.3.4");
        var actual = new AssemblyName("Dependency, Version=1.2.3.4");

        AssemblyCompatibilityValidator.ValidateReference("Consumer", expected, actual);
    }

    [Fact]
    public void ValidateReference_RejectsAssemblyVersionSkewWithGuidance()
    {
        var expected = new AssemblyName("Dependency, Version=1.2.3.4");
        var actual = new AssemblyName("Dependency, Version=1.2.4.0");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => AssemblyCompatibilityValidator.ValidateReference("Consumer", expected, actual));

        Assert.Contains("requires 'Dependency' assembly version '1.2.3.4'", exception.Message);
        Assert.Contains("version '1.2.4.0' is loaded", exception.Message);
        Assert.Contains("exact package versions", exception.Message);
    }
}
