// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class ExtensionsCsprojGenerator
    {
        internal const string ExtensionsProjectName = "WorkerExtensions.csproj";

        private readonly IDictionary<string, string> _extensions;
        private readonly string _outputPath;
        private readonly string _targetFrameworkIdentifier;
        private readonly string _targetFrameworkVersion;
        private readonly string _azureFunctionsVersion;

        public ExtensionsCsprojGenerator(IDictionary<string, string> extensions, string outputPath, string azureFunctionsVersion, string targetFrameworkIdentifier, string targetFrameworkVersion)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            _targetFrameworkIdentifier = targetFrameworkIdentifier ?? throw new ArgumentNullException(nameof(targetFrameworkIdentifier));
            _targetFrameworkVersion = targetFrameworkVersion ?? throw new ArgumentNullException(nameof(targetFrameworkVersion));
            _azureFunctionsVersion = azureFunctionsVersion ?? throw new ArgumentNullException(nameof(azureFunctionsVersion));
        }

        public void Generate()
        {
            var extensionsCsprojFilePath = Path.Combine(_outputPath, ExtensionsProjectName);

            string csproj = GetCsProjContent();
            if (File.Exists(extensionsCsprojFilePath))
            {
                string existing = File.ReadAllText(extensionsCsprojFilePath);
                if (string.Equals(csproj, existing, StringComparison.Ordinal))
                {
                    // If contents are the same, only touch the file to update timestamp.
                    File.SetLastWriteTimeUtc(extensionsCsprojFilePath, DateTime.UtcNow);
                    return;
                }
            }

            RecreateDirectory(_outputPath);
            File.WriteAllText(extensionsCsprojFilePath, csproj);
        }

        private void RecreateDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }

            Directory.CreateDirectory(directoryPath);
        }

        internal string GetCsProjContent()
        {
            string extensionReferences = GetExtensionReferences();
            string targetFramework = Constants.Net80;

            if (_targetFrameworkIdentifier.Equals(Constants.NetCoreApp, StringComparison.OrdinalIgnoreCase))
            {
                if (_azureFunctionsVersion.StartsWith(Constants.AzureFunctionsVersion3, StringComparison.OrdinalIgnoreCase) || _targetFrameworkVersion.Equals(Constants.NetCoreVersion31, StringComparison.OrdinalIgnoreCase))
                {
                    targetFramework = Constants.NetCoreApp31;
                }
            }

            string netSdkVersion = _azureFunctionsVersion.StartsWith(Constants.AzureFunctionsVersion3, StringComparison.OrdinalIgnoreCase) ? "3.1.2" : "4.6.0";

            return $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <Configuration>Release</Configuration>
        <AssemblyName>Microsoft.Azure.Functions.Worker.Extensions</AssemblyName>
        <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include=""Microsoft.NETCore.Targets"" Version=""3.0.0"" PrivateAssets=""all"" />
        <PackageReference Include=""Microsoft.NET.Sdk.Functions"" Version=""{netSdkVersion}"" />
{extensionReferences}    </ItemGroup>

    <Target Name=""_VerifyTargetFramework"" BeforeTargets=""Build"">
        <!-- It is possible to override our TFM via global properties. This can lead to successful builds, but runtime errors due to incompatible dependencies being brought in. -->
        <Error Condition=""'$(TargetFramework)' != '{targetFramework}'"" Text=""The target framework '$(TargetFramework)' must be '{targetFramework}'. Verify if target framework has been overridden by a global property."" />
    </Target>
</Project>
";
        }

        private string GetExtensionReferences()
        {
            var packages = new StringBuilder();

            foreach (KeyValuePair<string, string> extensionPair in _extensions)
            {
                packages.AppendLine(GetPackageReferenceFromExtension(name: extensionPair.Key, version: extensionPair.Value));
            }

            return packages.ToString();
        }

        private static string GetPackageReferenceFromExtension(string name, string version)
        {
            return $"        <PackageReference Include=\"{name}\" Version=\"{version}\" />";
        }
    }
}
