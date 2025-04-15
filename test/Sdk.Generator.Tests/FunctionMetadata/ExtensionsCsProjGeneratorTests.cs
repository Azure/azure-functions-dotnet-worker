// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public sealed class ExtensionsCsProjGeneratorTests : IDisposable
    {
        private HashSet<string?> _directoriesToCleanup = new();

        public enum FuncVersion
        {
            V3,
            V4,
        }

        public void Dispose()
        {
            foreach (string? directory in _directoriesToCleanup)
            {
                if (directory is null)
                {
                    continue;
                }

                if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
            }

            _directoriesToCleanup.Clear();
        }

        [Theory]
        [InlineData(FuncVersion.V3, true)]
        [InlineData(FuncVersion.V3, false)]
        [InlineData(FuncVersion.V4, true)]
        [InlineData(FuncVersion.V4, false)]
        public void GetCsProjContent_Succeeds(FuncVersion version, bool disclaimer)
        {
            string? disclaimerText = disclaimer ? "<!-- This is a test disclaimer. -->" : null;
            var generator = GetGenerator(version, "TestExtension.csproj", disclaimerText);
            string actual = generator.GetCsProjContent().Replace("\r\n", "\n");
            string expected = ExpectedCsproj(version, disclaimerText).Replace("\r\n", "\n");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(FuncVersion.V3)]
        [InlineData(FuncVersion.V4)]
        public void Generate_IncrementalSupport(FuncVersion version)
        {
            DateTime RunGenerate(string project, out string contents)
            {
                _directoriesToCleanup.Add(Path.GetDirectoryName(project));
                var generator = GetGenerator(version, project);
                generator.Generate();

                contents = File.ReadAllText(project);
                var csproj = new FileInfo(project);
                return csproj.LastWriteTimeUtc;
            }

            string project = Path.Combine(Guid.NewGuid().ToString(), "TestExtension.csproj");
            DateTime firstRun = RunGenerate(project, out string first);
            DateTime secondRun = RunGenerate(project, out string second);

            Assert.NotEqual(firstRun, secondRun);
            Assert.Equal(first, second);
        }

        [Fact]
        public async Task Generate_Updates()
        {
            DateTime RunGenerate(string project, IDictionary<string, string> extensions, out string contents)
            {
                _directoriesToCleanup.Add(Path.GetDirectoryName(project));
                var generator = GetGenerator(FuncVersion.V4, project, extensions);
                generator.Generate();

                contents = File.ReadAllText(project);
                var csproj = new FileInfo(project);
                return csproj.LastWriteTimeUtc;
            }

            Dictionary<string, string> extensions = new()
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.3" },
                { "Microsoft.Azure.WebJobs.Extensions.Http", "3.0.0" },
                { "Microsoft.Azure.WebJobs.Extensions", "2.0.0" },
            };

            string project = Path.Combine(Guid.NewGuid().ToString(), "TestExtension.csproj");
            DateTime firstRun = RunGenerate(project, extensions, out string first);

            await Task.Delay(10); // to ensure timestamps progress.
            extensions.Remove(extensions.Keys.First());
            DateTime secondRun = RunGenerate(project, extensions, out string second);

            Assert.NotEqual(firstRun.Ticks, secondRun.Ticks);
            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task Generate_Subdirectory_CreatesAll()
        {
            DateTime RunGenerate(string project, out string contents)
            {
                _directoriesToCleanup.Add(Path.GetDirectoryName(project));
                var generator = GetGenerator(FuncVersion.V4, project);
                generator.Generate();

                contents = File.ReadAllText(project);
                var csproj = new FileInfo(project);
                return csproj.LastWriteTimeUtc;
            }

            DateTime earliest = DateTime.UtcNow;

            await Task.Delay(10);
            string project = Path.Combine(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "TestExtension.csproj");
            DateTime time = RunGenerate(project, out string contents);

            Assert.True(time.Ticks >= earliest.Ticks, $"expected last write time {time.Ticks} to be greater than {earliest.Ticks}.");
            Assert.NotNull(contents);
        }

        [Fact]
        public async Task Generate_Subdirectory_CreatesPartial()
        {
            DateTime RunGenerate(string project, out string contents)
            {
                _directoriesToCleanup.Add(Path.GetDirectoryName(project));
                var generator = GetGenerator(FuncVersion.V4, project);
                generator.Generate();

                contents = File.ReadAllText(project);
                var csproj = new FileInfo(project);
                return csproj.LastWriteTimeUtc;
            }

            DateTime earliest = DateTime.UtcNow;
            string parent = Guid.NewGuid().ToString();
            Directory.CreateDirectory(parent);
            _directoriesToCleanup.Add(parent);

            await Task.Delay(10);
            string project = Path.Combine(parent, Guid.NewGuid().ToString(), "TestExtension.csproj");
            DateTime time = RunGenerate(project, out string contents);

            Assert.True(time.Ticks >= earliest.Ticks, $"expected last write time {time.Ticks} to be greater than {earliest.Ticks}.");
            Assert.NotNull(contents);
        }

        [Fact]
        public async Task Generate_ExistingDirectory_DoesNotOverwrite()
        {
            DateTime RunGenerate(string project, out string contents)
            {
                _directoriesToCleanup.Add(Path.GetDirectoryName(project));
                var generator = GetGenerator(FuncVersion.V4, project);
                generator.Generate();

                contents = File.ReadAllText(project);
                var csproj = new FileInfo(project);
                return csproj.LastWriteTimeUtc;
            }

            string parent = Guid.NewGuid().ToString();
            Directory.CreateDirectory(parent);
            _directoriesToCleanup.Add(parent);

            string existing = Path.Combine(parent, "existing.txt");
            File.WriteAllText(existing, "");
            DateTime expectedWriteTime = new FileInfo(existing).LastWriteTimeUtc;

            await Task.Delay(10);
            string project = Path.Combine(parent, "TestExtension.csproj");
            DateTime time = RunGenerate(project, out string contents);

            Assert.True(time.Ticks >= expectedWriteTime.Ticks, $"expected last write time {time.Ticks} to be greater than {expectedWriteTime.Ticks}.");
            Assert.NotNull(contents);
            Assert.Equal(expectedWriteTime, new FileInfo(existing).LastWriteTimeUtc);
        }

        static ExtensionsCsprojGenerator GetGenerator(FuncVersion version, string outputPath, string? disclaimer = null)
        {
            Dictionary<string, string> extensions = new()
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.3" },
                { "Microsoft.Azure.WebJobs.Extensions.Http", "3.0.0" },
                { "Microsoft.Azure.WebJobs.Extensions", "2.0.0" },
            };

            return GetGenerator(version, outputPath, extensions, disclaimer);
        }

        static ExtensionsCsprojGenerator GetGenerator(
            FuncVersion version, string outputPath, IDictionary<string, string> extensions, string? disclaimer = null)
        {
            return version switch
            {
                FuncVersion.V3 => new ExtensionsCsprojGenerator(extensions, outputPath, "v3", Constants.NetCoreApp, Constants.NetCoreVersion31)
                {
                    Disclaimer = disclaimer,
                },
                FuncVersion.V4 => new ExtensionsCsprojGenerator(extensions, outputPath, "v4", Constants.NetCoreApp, Constants.NetCoreVersion8)
                {
                    Disclaimer = disclaimer,
                },
                _ => throw new ArgumentOutOfRangeException(nameof(version)),
            };
        }

        private static string ExpectedCsproj(FuncVersion version, string? disclaimer = null)
            => version switch
            {
                FuncVersion.V3 => ExpectedCsProjV3(disclaimer),
                FuncVersion.V4 => ExpectedCsProjV4(disclaimer),
                _ => throw new ArgumentOutOfRangeException(nameof(version)),
            };

        private static string ExpectedCsProjV3(string? disclaimer)
        {
            return $@"{disclaimer}
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
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

        private static string ExpectedCsProjV4(string? disclaimer)
        {
            return $@"{disclaimer}
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>Microsoft.Azure.Functions.Worker.Extensions</AssemblyName>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NETCore.Targets"" Version=""3.0.0"" PrivateAssets=""all"" />
    <PackageReference Include=""Microsoft.NET.Sdk.Functions"" Version=""4.6.0"" />
    <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Storage"" Version=""4.0.3"" />
    <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions.Http"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Azure.WebJobs.Extensions"" Version=""2.0.0"" />
  </ItemGroup>

  <Target Name=""_VerifyTargetFramework"" BeforeTargets=""Build"">
    <!-- It is possible to override our TFM via global properties. This can lead to successful builds, but runtime errors due to incompatible dependencies being brought in. -->
    <Error Condition=""'$(TargetFramework)' != 'net8.0'"" Text=""The target framework '$(TargetFramework)' must be 'net8.0'. Verify if target framework has been overridden by a global property."" />
  </Target>
</Project>
";
        }
    }
}
