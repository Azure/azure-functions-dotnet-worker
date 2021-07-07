﻿using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Azure.Functions.SdkE2ETests;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.SdkTests
{
    public class ZipDeployTests
    {
        private ITestOutputHelper _testOutputHelper;

        public ZipDeployTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("linux-x64", true)]
        [InlineData("win-x64", false)]
        [InlineData("win-x64", true)]
        public async Task Zip(string rid, bool selfContained)
        {
            string outputDir = await TestUtility.InitializeTestAsync(_testOutputHelper, nameof(Zip));
            string directoryToZip = Path.Combine(outputDir, "buildoutput");
            Directory.CreateDirectory(directoryToZip);

            string zipName = Path.Combine(outputDir, $"{nameof(Zip)}_test.zip");

            await TestUtility.DeleteFileAsync(zipName);

            string projectFileDirectory = Path.Combine(TestUtility.SamplesRoot, "FunctionApp", "FunctionApp.csproj");

            await TestUtility.RestoreAndPublishProjectAsync(projectFileDirectory, directoryToZip, $"-r {rid} --self-contained {selfContained}", _testOutputHelper);

            CreateZipFileTask.CreateZipFileFromDirectory(directoryToZip, zipName);

            using (var zip = new ZipFile(zipName))
            {
                Assert.Equal(Directory.GetFiles(directoryToZip, "*", SearchOption.AllDirectories).Length, zip.Count);

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
}
