// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class ExtensionsCsprojGenerator
    {
        private const string ExtensionsProjectName = "WorkerExtensions.csproj";

        private readonly IDictionary<string, string> _extensions;
        private readonly string _outputPath;

        public ExtensionsCsprojGenerator(IDictionary<string, string> extensions, string outputPath)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        }

        public void Generate()
        {
            var extensionsCsprojFilePath = Path.Combine(_outputPath, ExtensionsProjectName);

            RecreateDirectory(_outputPath);

            WriteExtensionsCsProj(extensionsCsprojFilePath);
        }

        private void RecreateDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }

            Directory.CreateDirectory(directoryPath);
        }

        private void WriteExtensionsCsProj(string filePath)
        {
            string csprojContent = GetCsProjContent();

            File.WriteAllText(filePath, csprojContent);
        }

        internal string GetCsProjContent()
        {
            string extensionReferences = GetExtensionReferences();

            return $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
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
        <PackageReference Include=""Microsoft.Azure.WebJobs.Script.ExtensionsMetadataGenerator"" Version=""1.2.0"" />
        {extensionReferences}
    </ItemGroup>
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
            return $@"<PackageReference Include=""{name}"" Version=""{version}"" />";
        }
    }
}
