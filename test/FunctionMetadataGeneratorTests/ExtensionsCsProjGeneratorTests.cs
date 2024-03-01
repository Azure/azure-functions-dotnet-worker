﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Sdk;
using Xunit;

namespace Microsoft.Azure.Functions.SdkTests
{
    public class ExtensionsCsProjGeneratorTests
    {
        [Fact]
        public void GetCsProjContent_Succeeds_functions_v3()
        {
            IDictionary<string, string> extensions = new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.3" },
                { "Microsoft.Azure.WebJobs.Extensions.Http", "3.0.0" },
                { "Microsoft.Azure.WebJobs.Extensions", "2.0.0" },
            };

            var generator = new ExtensionsCsprojGenerator(extensions, "", "v3", Constants.NetCoreApp, Constants.NetCoreVersion31);

            string actualCsproj = generator.GetCsProjContent().Replace("\r\n", "\n");

            Assert.Equal(ExpectedCsProjV3(), actualCsproj);
        }

        private static string ExpectedCsProjV3()
        {
            return @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>netcoreapp3.1</TargetFramework>
        <Configuration>Release</Configuration>
        <AssemblyName>Microsoft.Azure.Functions.Worker.Extensions</AssemblyName>
        <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include=""Microsoft.NETCore.Targets"" Version=""3.0.0"" PrivateAssets=""all"" />
        <PackageReference Include=""Microsoft.NET.Sdk.Functions"" Version=""3.1.2"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Storage"" Version=""4.0.3"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Http"" Version=""3.0.0"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions"" Version=""2.0.0"" />
    </ItemGroup>

    <Target Name=""_VerifyTargetFramework"" BeforeTargets=""Build"">
        <!-- It is possible to override our TFM via global properties. This can lead to successful builds, but runtime errors due to incompatible dependencies being brought in. -->
        <Error Condition=""'$(TargetFramework)' != 'netcoreapp3.1'"" Text=""The target framework '$(TargetFramework)' must be 'netcoreapp3.1'. Verify if target framework has been overridden by a global property."" />
    </Target>
</Project>
";
        }

        [Fact]
        public void GetCsProjContent_Succeeds_functions_v4()
        {
            IDictionary<string, string> extensions = new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.3" },
                { "Microsoft.Azure.WebJobs.Extensions.Http", "3.0.0" },
                { "Microsoft.Azure.WebJobs.Extensions", "2.0.0" },
            };

            var generator = new ExtensionsCsprojGenerator(extensions, "", "v4", Constants.NetCoreApp, Constants.NetCoreVersion6);

            string actualCsproj = generator.GetCsProjContent().Replace("\r\n", "\n");

            Assert.Equal(ExpectedCsProjV4(), actualCsproj);
        }

        private static string ExpectedCsProjV4()
        {
            return @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <Configuration>Release</Configuration>
        <AssemblyName>Microsoft.Azure.Functions.Worker.Extensions</AssemblyName>
        <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include=""Microsoft.NETCore.Targets"" Version=""3.0.0"" PrivateAssets=""all"" />
        <PackageReference Include=""Microsoft.NET.Sdk.Functions"" Version=""4.3.0"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Storage"" Version=""4.0.3"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Http"" Version=""3.0.0"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions"" Version=""2.0.0"" />
    </ItemGroup>

    <Target Name=""_VerifyTargetFramework"" BeforeTargets=""Build"">
        <!-- It is possible to override our TFM via global properties. This can lead to successful builds, but runtime errors due to incompatible dependencies being brought in. -->
        <Error Condition=""'$(TargetFramework)' != 'net6.0'"" Text=""The target framework '$(TargetFramework)' must be 'net6.0'. Verify if target framework has been overridden by a global property."" />
    </Target>
</Project>
";
        }
    }
}
