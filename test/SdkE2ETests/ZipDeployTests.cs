using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Azure.Functions.SdkE2ETests;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkTests
{
    public class ZipDeployTests(ITestOutputHelper testOutputHelper)
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

            for (int i = 0; i < zip.Count; i++)
            {
                var entry = zip[i];
                if (selfContained &&
                    (entry.Name == "FunctionApp" || entry.Name == "FunctionApp.exe"))
                {
                    Assert.Equal(3, entry.HostSystem);
                    Assert.Equal(CreateZipFileTask.UnixExecutablePermissions, entry.ExternalFileAttributes);
                }
                else
                {
                    Assert.Equal(0, entry.HostSystem);
                    Assert.Equal(0, entry.ExternalFileAttributes);
                }
            }

            zip.Close();
        }
    }
}
