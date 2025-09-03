// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Sdk;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public class ExtensionsMetadataEnhancerTests
    {
        [Fact]
        public void AddHintPath_AddsExtensionsPath()
        {
            var extensions = GetBasicReferences_WithoutHintPath();
            var extensionsWithHints = GetBasicReferences_WithExtensionsHintPath();

            ValidateHintPathUnequal(extensions, extensionsWithHints);

            ExtensionsMetadataEnhancer.AddHintPath(extensions);

            ValidateAllEqual(extensionsWithHints, extensions);
        }

        [Fact]
        public void AddHintPath_DoesNotAdd_WhenAlreadyPresent()
        {
            var extensionsPreset = GetBasicReferences_WithPresetHintPath();
            var extensionsCorrectedHints = GetBasicReferences_WithExtensionsHintPath();

            ValidateHintPathUnequal(extensionsCorrectedHints, extensionsPreset);

            ExtensionsMetadataEnhancer.AddHintPath(extensionsPreset);

            ValidateHintPathUnequal(extensionsCorrectedHints, extensionsPreset);
            ValidateAllEqual(GetBasicReferences_WithPresetHintPath(), extensionsPreset);
        }

        [Fact]
        public void GetWebJobsExtensions_FindsExtensions()
        {
            string assembly = Path.Combine(TestUtility.RepoRoot, "out", "bin", "src", "FunctionMetadataLoaderExtension", TestUtility.Config, "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll");
            var extensions = ExtensionsMetadataEnhancer.GetWebJobsExtensions(assembly);

            ValidateAllEqual(
                [
                    new ExtensionReference()
                    {
                        Name = "Startup",
                        TypeName = "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c",
                        HintPath = "./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll",
                    }
                ],
                extensions);
        }

        private static void ValidateAllEqual(IEnumerable<ExtensionReference> expected, IEnumerable<ExtensionReference> actual)
        {
            Assert.Equal(expected.Count(), actual.Count());

            using IEnumerator<ExtensionReference> expectedEnum = expected.GetEnumerator();
            using IEnumerator<ExtensionReference> actualEnum = actual.GetEnumerator();
            while (expectedEnum.MoveNext() && actualEnum.MoveNext())
            {
                Assert.Equal(expectedEnum.Current.Name, actualEnum.Current.Name);
                Assert.Equal(expectedEnum.Current.TypeName, actualEnum.Current.TypeName);
                Assert.Equal(expectedEnum.Current.HintPath, actualEnum.Current.HintPath);
            }
        }

        private static void ValidateHintPathUnequal(IEnumerable<ExtensionReference> expected, IEnumerable<ExtensionReference> actual)
        {
            Assert.Equal(expected.Count(), actual.Count());

            using IEnumerator<ExtensionReference> expectedEnum = expected.GetEnumerator();
            using IEnumerator<ExtensionReference> actualEnum = actual.GetEnumerator();
            while (expectedEnum.MoveNext() && actualEnum.MoveNext())
            {
                Assert.Equal(expectedEnum.Current.Name, actualEnum.Current.Name);
                Assert.Equal(expectedEnum.Current.TypeName, actualEnum.Current.TypeName);
                Assert.NotEqual(expectedEnum.Current.HintPath, actualEnum.Current.HintPath);
            }
        }

        private static IEnumerable<ExtensionReference> GetBasicReferences_WithoutHintPath()
        {
            return new List<ExtensionReference>
            {
                new ExtensionReference() { 
                    Name = "MySecretExtension", 
                    TypeName = "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c"
                },
                new ExtensionReference() {
                    Name = "AnotherExtension",
                    TypeName = "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage, Version=4.0.3.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
                },
                new ExtensionReference() {
                    Name = "SomeRandom",
                    TypeName = "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageWebJobsStartup, Yada.Foo.Yada.Yada.Bar, Version=4.0.3.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
                }
            };
        }

        private static IEnumerable<ExtensionReference> GetBasicReferences_WithExtensionsHintPath()
        {
            return GetBasicReferences_WithHintPath(".azurefunctions");
        }

        private static IEnumerable<ExtensionReference> GetBasicReferences_WithPresetHintPath()
        {
            return GetBasicReferences_WithHintPath("somePresetDirectory");
        }

        private static IEnumerable<ExtensionReference> GetBasicReferences_WithHintPath(string baseDir)
        {
            return new List<ExtensionReference>
            {
                new ExtensionReference() {
                    Name = "MySecretExtension",
                    TypeName = "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c",
                    HintPath = $"./{baseDir}/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll"
                },
                new ExtensionReference() {
                    Name = "AnotherExtension",
                    TypeName = "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage, Version=4.0.3.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    HintPath = $"./{baseDir}/Microsoft.Azure.WebJobs.Extensions.Storage.dll"
                },
                new ExtensionReference() {
                    Name = "SomeRandom",
                    TypeName = "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageWebJobsStartup, Yada.Foo.Yada.Yada.Bar, Version=4.0.3.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    HintPath = $"./{baseDir}/Yada.Foo.Yada.Yada.Bar.dll"
                }
            };
        }
    }
}
