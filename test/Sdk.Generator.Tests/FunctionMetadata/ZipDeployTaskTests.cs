// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.Tasks;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public class ZipDeployTaskTests
    {
        private static string? _testZippedPublishContentsPath;
        private const string TestAssemblyToTestZipPath = @"Resources/TestPublishContents.zip";
        private const string UserAgentName = "functions-core-tools";
        private const string UserAgentVersion = "1.0";

        public static string TestZippedPublishContentsPath
        {
            get
            {
                if (_testZippedPublishContentsPath == null)
                {
                    string codebase = typeof(ZipDeployTaskTests).Assembly.Location;
                    string assemblyPath = new Uri(codebase, UriKind.Absolute).LocalPath;
                    string baseDirectory = Path.GetDirectoryName(assemblyPath)!;
                    _testZippedPublishContentsPath = Path.Combine(baseDirectory, TestAssemblyToTestZipPath);
                }

                return _testZippedPublishContentsPath;
            }
        }

        [Fact]
        public async Task ExecuteZipDeploy_InvalidZipFilePath()
        {
            Mock<IHttpClient> client = new Mock<IHttpClient>();
            ZipDeployTask zipDeployer = new ZipDeployTask();

            bool result = await zipDeployer.ZipDeployAsync(string.Empty, "username", "password", "publishUrl", null, "Foo", false, client.Object, false);

            client.Verify(c => c.PostAsync(It.IsAny<Uri>(), It.IsAny<StreamContent>()), Times.Never);
            Assert.False(result);
        }

        /// <summary>
        /// ZipDeploy should use PublishUrl if not null or empty, else use SiteName.
        /// </summary>
        [Theory]
        [InlineData("https://sitename.scm.azurewebsites.net", null, false, "https://sitename.scm.azurewebsites.net/api/zipdeploy?isAsync=true")]
        [InlineData("https://sitename.scm.azurewebsites.net", null, true, "https://sitename.scm.azurewebsites.net/api/publish?RemoteBuild=false")]
        [InlineData("https://sitename.scm.azurewebsites.net", "", false, "https://sitename.scm.azurewebsites.net/api/zipdeploy?isAsync=true")]
        [InlineData("https://sitename.scm.azurewebsites.net", "", true, "https://sitename.scm.azurewebsites.net/api/publish?RemoteBuild=false")]
        [InlineData("https://sitename.scm.azurewebsites.net", "shouldNotBeUsed", false, "https://sitename.scm.azurewebsites.net/api/zipdeploy?isAsync=true")]
        [InlineData("https://sitename.scm.azurewebsites.net", "shouldNotBeUsed", true, "https://sitename.scm.azurewebsites.net/api/publish?RemoteBuild=false")]
        [InlineData(null, "sitename", false, "https://sitename.scm.azurewebsites.net/api/zipdeploy?isAsync=true")]
        [InlineData(null, "sitename", true, "https://sitename.scm.azurewebsites.net/api/publish?RemoteBuild=false")]
        [InlineData("", "sitename", false, "https://sitename.scm.azurewebsites.net/api/zipdeploy?isAsync=true")]
        [InlineData("", "sitename", true, "https://sitename.scm.azurewebsites.net/api/publish?RemoteBuild=false")]
        public async Task ExecuteZipDeploy_PublishUrlOrSiteNameGiven(string? publishUrl, string? siteName, bool useBlobContainerDeploy, string expectedZipDeployEndpoint)
        {
            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.PostAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, expectedZipDeployEndpoint, StringComparison.Ordinal)),
                It.Is<StreamContent>(streamContent => IsStreamContentEqualToFileContent(streamContent, TestZippedPublishContentsPath))),
                Times.Once);
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.True(result);
            };

            await RunZipDeployAsyncTest(publishUrl, siteName, UserAgentVersion, useBlobContainerDeploy, HttpStatusCode.OK, verifyStep);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("", null)]
        [InlineData(null, "")]
        public async Task ExecuteZipDeploy_NeitherPublishUrlNorSiteNameGiven(string? publishUrl, string? siteName)
        {
            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.PostAsync(
                It.IsAny<Uri>(),
                It.IsAny<StreamContent>()),
                Times.Never);
                Assert.False(client.Object.DefaultRequestHeaders.TryGetValues("User-Agent", out _));
                Assert.False(result);
            };

            await RunZipDeployAsyncTest(publishUrl, siteName, UserAgentVersion, useBlobContainerDeploy: false, HttpStatusCode.OK, verifyStep);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, false, true)]
        [InlineData(HttpStatusCode.OK, true, true)]
        [InlineData(HttpStatusCode.Accepted, false, true)]
        [InlineData(HttpStatusCode.Accepted, true, true)]
        [InlineData(HttpStatusCode.Forbidden, false, false)]
        [InlineData(HttpStatusCode.Forbidden, true, false)]
        [InlineData(HttpStatusCode.NotFound, false, false)]
        [InlineData(HttpStatusCode.NotFound, true, false)]
        [InlineData(HttpStatusCode.RequestTimeout, false, false)]
        [InlineData(HttpStatusCode.RequestTimeout, true, false)]
        [InlineData(HttpStatusCode.InternalServerError, false, false)]
        [InlineData(HttpStatusCode.InternalServerError, true, false)]
        public async Task ExecuteZipDeploy_VaryingHttpResponseStatuses(
            HttpStatusCode responseStatusCode, bool useBlobContainerDeploy, bool expectedResult)
        {
            var zipDeployPublishUrl = useBlobContainerDeploy
                ? "https://sitename.scm.azurewebsites.net/api/publish?RemoteBuild=false"
                : "https://sitename.scm.azurewebsites.net/api/zipdeploy?isAsync=true";

            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.PostAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, zipDeployPublishUrl, StringComparison.Ordinal)),
                It.Is<StreamContent>(streamContent => IsStreamContentEqualToFileContent(streamContent, TestZippedPublishContentsPath))),
                Times.Once);
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.Equal(expectedResult, result);
            };

            await RunZipDeployAsyncTest("https://sitename.scm.azurewebsites.net", null, UserAgentVersion, useBlobContainerDeploy, responseStatusCode, verifyStep);
        }

        private async Task RunZipDeployAsyncTest(string? publishUrl, string? siteName, string userAgentVersion, bool useBlobContainerDeploy, HttpStatusCode responseStatusCode, Action<Mock<IHttpClient>, bool> verifyStep)
        {
            Mock<IHttpClient> client = new Mock<IHttpClient>();

            //constructing HttpRequestMessage to get HttpRequestHeaders as HttpRequestHeaders contains no public constructors
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.PostAsync(It.IsAny<Uri>(), It.IsAny<StreamContent>())).Returns((Uri uri, StreamContent streamContent) =>
            {
                byte[] plainAuthBytes = Encoding.ASCII.GetBytes("username:password");
                string base64AuthParam = Convert.ToBase64String(plainAuthBytes);

                Assert.Equal(base64AuthParam, client.Object.DefaultRequestHeaders.Authorization!.Parameter);
                Assert.Equal("Basic", client.Object.DefaultRequestHeaders.Authorization.Scheme);

                return Task.FromResult(new HttpResponseMessage(responseStatusCode));
            });

            Func<Uri, StreamContent, Task<HttpResponseMessage>> runPostAsync = (uri, streamContent) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            };

            ZipDeployTask zipDeployer = new ZipDeployTask();

            bool result = await zipDeployer.ZipDeployAsync(TestZippedPublishContentsPath, "username", "password", publishUrl, siteName, userAgentVersion, useBlobContainerDeploy, client.Object, false);

            verifyStep(client, result);
        }

        private bool IsStreamContentEqualToFileContent(StreamContent streamContent, string filePath)
        {
            byte[] expectedZipByteArr = File.ReadAllBytes(filePath);
            Task<byte[]> t = streamContent.ReadAsByteArrayAsync();
            t.Wait();
            return expectedZipByteArr.SequenceEqual(t.Result);
        }
    }
}
