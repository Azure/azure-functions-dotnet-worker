// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.



using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Resolver;

public class TestSdkResolver : SdkResolver
{
    private static readonly string SdkPath = Path.Combine(
        Path.GetDirectoryName(typeof(TestSdkResolver).Assembly.Location), "sdk");

    public override string Name => "Test Azure Functions SDK Resolver";

    public override int Priority => 1000;

    public override SdkResult Resolve(SdkReference sdkReference, SdkResolverContext context, SdkResultFactory factory)
    {
        if (sdkReference.Name == "Azure.Functions.Sdk")
        {
            return factory.IndicateSuccess(SdkPath, "99.99.99"); // test version.
        }

        return factory.IndicateFailure([]);
    }
}
