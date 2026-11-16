// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Resolver;

public class TestSdkResolver : SdkResolver
{
    private const string TestVersion = "99.99.99";

    private static readonly string SdkPath = Path.Combine(
        Path.GetDirectoryName(GetAssemblyLocation()), "sdk");

    // Evaluated before Sdk.props, so the SDK reports the same version this resolver does.
    private static readonly Dictionary<string, string> SdkProperties = new()
    {
        ["AzureFunctionsSdkVersion"] = TestVersion,
    };

    public override string Name => "Test Azure Functions SDK Resolver";

    public override int Priority => 1000;

    public override SdkResult Resolve(SdkReference sdkReference, SdkResolverContext context, SdkResultFactory factory)
    {
        if (sdkReference.Name == "Azure.Functions.Sdk")
        {
            return factory.IndicateSuccess(SdkPath, TestVersion, SdkProperties, itemsToAdd: null);
        }

        return factory.IndicateFailure([]);
    }

    private static string GetAssemblyLocation()
    {
        // Using codebase and not location due to shadow copying when running netfx tests.
        Uri uri = new Uri(typeof(TestSdkResolver).Assembly.CodeBase!);
        return uri.AbsolutePath;
    }
}
