// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Sdk.Tests;

public class FunctionsAssemblyScannerTests
{
    public static TheoryData<string> ShouldScanPackageFalseData =>
        new()
        {
            "System.Text.Json",
            "Azure.Core",
            "Azure.Identity",
            "Microsoft.Bcl.AsyncInterfaces",
            "Microsoft.Extensions.Azure",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Identity.Client",
            "Microsoft.NETCore.Platforms",
            "Microsoft.NETStandard.Library",
            "Microsoft.Win32.Registry",
            "Grpc.AspNetCore",
            "Grpc.Net.Client",
        };

    public static TheoryData<string> ShouldScanPackageTrueData =>
        new()
        {
            "Azure.CoreOther",
            "SystemSomethingElse",
            "Microsoft.BclOther",
            "Microsoft.Azure.Functions.Worker.Extensions",
            "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus",
            "Some.Custom.Package",
        };

    [Theory]
    [MemberData(nameof(ShouldScanPackageFalseData))]
    public void ShouldScanPackage_ReturnsFalse(string assembly)
    {
        bool result = FunctionsAssemblyScanner.ShouldScanPackage(assembly);
        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ShouldScanPackageTrueData))]
    public void ShouldScanPackage_ReturnsTrue(string assembly)
    {
        bool result = FunctionsAssemblyScanner.ShouldScanPackage(assembly);
        result.Should().BeTrue();
    }
}
