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
        public void GetCsProjContent_Succeeds()
        {
            IDictionary<string, string> extensions = new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.3" },
                { "Microsoft.Azure.WebJobs.Extensions.Http", "3.0.0" },
                { "Microsoft.Azure.WebJobs.Extensions", "2.0.0" },
            };

            var generator = new ExtensionsCsprojGenerator(extensions, "", "netcoreapp3.1");

            string actualCsproj = generator.GetCsProjContent();

            Assert.Equal(ExpectedCsProj(), actualCsproj);
        }

        private static string ExpectedCsProj()
        {
            return @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>netcoreapp3.1</TargetFramework>
        <LangVersion>preview</LangVersion>
        <Configuration>Release</Configuration>
        <AssemblyName>Microsoft.Azure.Functions.Worker.Extensions</AssemblyName>
        <RootNamespace>Microsoft.Azure.Functions.Worker.Extensions</RootNamespace>
        <MajorMinorProductVersion>1.0</MajorMinorProductVersion>
        <Version>$(MajorMinorProductVersion).0</Version>
        <AssemblyVersion>$(MajorMinorProductVersion).0.0</AssemblyVersion>
        <FileVersion>$(Version)</FileVersion>
        <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include=""Microsoft.NETCore.Targets"" Version=""3.0.0"" PrivateAssets=""all"" />
        <PackageReference Include=""Microsoft.NET.Sdk.Functions"" Version=""3.0.11"" />
        <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Storage"" Version=""4.0.3"" />
<PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Http"" Version=""3.0.0"" />
<PackageReference Include=""Microsoft.Azure.WebJobs.Extensions"" Version=""2.0.0"" />

    </ItemGroup>
</Project>
";
        }
    }
}
