// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public class ZipDeploymentStatusTests
    {
        private const string UserAgentName = "functions-core-tools";
        private const string UserAgentVersion = "1.0";
        private const string userName = "deploymentUser";
        private const string password = "deploymentPassword";
        private const string DeploymentResponse = @"{
    ""id"": ""7010fa61-d5df-46b5-a22e-98cfc81f1637"",
    ""status"": 3,
    ""status_text"": """",
    ""author_email"": ""N/A"",
    ""author"": ""N/A"",
    ""deployer"": ""Push-Deployer"",
    ""message"": ""Created via a push deployment"",
    ""progress"": """",
    ""received_time"": ""2024-09-10T04:40:36.0994691Z"",
    ""start_time"": ""2024-09-10T04:40:37.1272389Z"",
    ""end_time"": ""2024-09-10T04:40:39.4733696Z"",
    ""last_success_end_time"": null,
    ""complete"": true,
    ""active"": false,
    ""is_temp"": false,
    ""is_readonly"": true,
    ""url"": ""https://testFunctionApp.scm.azurewebsites.net/api/deployments/latest"",
    ""log_url"": ""https://testFuncitonApp.scm.azurewebsites.net/api/deployments/latest/log"",
    ""site_name"": ""testFunctionApp"",
    ""build_summary"": {
        ""errors"": [],
        ""warnings"": []
    }
}";

        private readonly TaskLoggingHelper _log = new(Mock.Of<IBuildEngine>(), "test");

        [Theory]
        [InlineData(HttpStatusCode.Forbidden, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.NotFound, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.RequestTimeout, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.InternalServerError, DeployStatus.Unknown)]
        public async Task PollDeploymentStatusTest_ForErrorResponses(HttpStatusCode responseStatusCode, DeployStatus expectedDeployStatus)
        {
            // Arrange
            string deployUrl = "https://sitename.scm.azurewebsites.net/DeploymentStatus?Id=knownId";
            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.GetAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, deployUrl, StringComparison.Ordinal)), It.IsAny<CancellationToken>()));
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.True(result);
            };

            Mock<IHttpClient> client = new Mock<IHttpClient>();
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.GetAsync(new Uri(deployUrl, UriKind.RelativeOrAbsolute), It.IsAny<CancellationToken>())).Returns(() =>
            {
                return Task.FromResult(new HttpResponseMessage(responseStatusCode));
            });

            ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client.Object, $"{UserAgentName}/{UserAgentVersion}", _log, false);

            // Act
            var actualdeployStatus = await deploymentStatus.PollDeploymentStatusAsync(deployUrl, userName, password);

            // Assert
            verifyStep(client, expectedDeployStatus == actualdeployStatus);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "", DeployStatus.Success)]
        [InlineData(HttpStatusCode.Accepted, null, DeployStatus.Success)]
        [InlineData(HttpStatusCode.OK, "", DeployStatus.PartialSuccess)]
        [InlineData(HttpStatusCode.Accepted, "Operation succeeded partially", DeployStatus.PartialSuccess)]
        [InlineData(HttpStatusCode.OK, "Instance configuration is not valid", DeployStatus.Failed)]
        [InlineData(HttpStatusCode.Accepted, "", DeployStatus.Failed)]
        [InlineData(HttpStatusCode.OK, "Conflicting changes exist", DeployStatus.Conflict)]
        [InlineData(HttpStatusCode.Accepted, "", DeployStatus.Conflict)]
        [InlineData(HttpStatusCode.OK, null, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.Accepted, null, DeployStatus.Unknown)]
        public async Task PollDeploymentStatusTest_ForValidResponses(HttpStatusCode responseStatusCode, string? statusMessage, DeployStatus expectedDeployStatus)
        {
            // Arrange
            string deployUrl = "https://sitename.scm.azurewebsites.net/DeploymentStatus?Id=knownId";
            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.GetAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, deployUrl, StringComparison.Ordinal)), It.IsAny<CancellationToken>()));
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.True(result);
            };

            Mock<IHttpClient> client = new Mock<IHttpClient>();
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.GetAsync(new Uri(deployUrl, UriKind.RelativeOrAbsolute), It.IsAny<CancellationToken>())).Returns(() =>
            {
                string statusJson = JsonConvert.SerializeObject(new
                {
                    status = expectedDeployStatus,
                    status_text = statusMessage
                }, Formatting.Indented);

                HttpContent httpContent = new StringContent(statusJson, Encoding.UTF8, "application/json");
                HttpResponseMessage responseMessage = new HttpResponseMessage(responseStatusCode)
                {
                    Content = httpContent
                };
                return Task.FromResult(responseMessage);
            });
            ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client.Object, $"{UserAgentName}/{UserAgentVersion}", _log, false);

            // Act
            var actualdeployStatus = await deploymentStatus.PollDeploymentStatusAsync(deployUrl, userName, password);

            // Assert
            verifyStep(client, expectedDeployStatus == actualdeployStatus);
        }

        [Fact]
        public async Task PollDeploymentStatusTest_WithDeploymentSummary_Succeeds()
        {
            // Arrange
            string deployUrl = "https://sitename.scm.azurewebsites.net/DeploymentStatus?Id=knownId";
            Action<Mock<IHttpClient>, DeployStatus> verifyStep = (client, status) =>
            {
                client.Verify(c => c.GetAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, deployUrl, StringComparison.Ordinal)), It.IsAny<CancellationToken>()));
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.Equal(DeployStatus.Failed, status);
            };

            Mock<IHttpClient> client = new Mock<IHttpClient>();
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.GetAsync(new Uri(deployUrl, UriKind.RelativeOrAbsolute), It.IsAny<CancellationToken>())).Returns(() =>
            {
                HttpContent httpContent = new StringContent(DeploymentResponse, Encoding.UTF8, "application/json");
                HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = httpContent
                };
                return Task.FromResult(responseMessage);
            });

            ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client.Object, $"{UserAgentName}/{UserAgentVersion}", _log, false);

            // Act
            var actualdeployStatus = await deploymentStatus.PollDeploymentStatusAsync(deployUrl, userName, password);

            // Assert
            verifyStep(client, actualdeployStatus);
        }
    }
}
