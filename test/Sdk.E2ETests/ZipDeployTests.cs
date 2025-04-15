using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Sdk.E2ETests
{
    public sealed class ZipDeployTests(ITestOutputHelper testOutputHelper) : IDisposable
    {
        private readonly ProjectBuilder _builder = new(
            testOutputHelper,
            Path.Combine(TestUtility.SamplesRoot, "FunctionApp", "FunctionApp.csproj"));

        [Theory]
        [InlineData("linux-x64", true)]
        [InlineData("win-x64", false)]
        [InlineData("win-x64", true)]
        public async Task CreateZipFileFromDirectory_SetsExecutableFlag_WhenSelfContained(string rid, bool selfContained)
        {
            string testName = nameof(CreateZipFileFromDirectory_SetsExecutableFlag_WhenSelfContained);
            string zipName = Path.Combine(Directory.GetParent(_builder.OutputPath).FullName, $"{testName}.zip");

            if (File.Exists(zipName))
            {
                File.Delete(zipName);
            }

            string projectFileDirectory = Path.Combine(TestUtility.SamplesRoot, "FunctionApp", "FunctionApp.csproj");

            await _builder.PublishAsync($"-r {rid} --self-contained {selfContained}");

            CreateZipFileTask.CreateZipFileFromDirectory(_builder.OutputPath, zipName);

            using var zip = new ZipFile(zipName);
            Assert.Equal(Directory.GetFiles(_builder.OutputPath, "*", SearchOption.AllDirectories).Length, zip.Count);

            foreach (ZipEntry entry in zip)
            {
                if (selfContained && (entry.Name == "FunctionApp" || entry.Name == "FunctionApp.exe"))
                {
                    Assert.Equal(3, entry.HostSystem);
                    Assert.Equal(CreateZipFileTask.UnixExecutablePermissions, entry.ExternalFileAttributes);
                }
                else if (OperatingSystem.IsWindows())
                {
                    // All other files are default on windows.
                    Assert.Equal(0, entry.HostSystem);
                    Assert.Equal(0, entry.ExternalFileAttributes);
                }
                else
                {
                    Assert.Equal(3, entry.HostSystem);

                    // Unix permissions will vary based on the file. Just making sure they have _some_ permissions
                    Assert.NotEqual(0, entry.ExternalFileAttributes);
                }
            }

            zip.Close();
        }

        public void Dispose() => _builder.Dispose();
    }
}
