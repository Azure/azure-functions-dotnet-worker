// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AwesomeAssertions.Execution;
using Azure.Functions.Sdk.ZipDeploy;
using Microsoft.Build.Framework;
using NuGet.Common;
using Task = System.Threading.Tasks.Task;

namespace Azure.Functions.Sdk.Tasks.Publish.Tests;

public sealed class ZipDeployTaskTests
{
    private const string TestZipPath = @"C:\publish\app.zip";
    private const string TestPublishUrl = "https://functionapp.scm.test/";
    private const string TestUsername = "deployUser";
    private const string TestPassword = "deployPass";

    private readonly MockFileSystem _fileSystem = new();
    private readonly Mock<IBuildEngine> _buildEngine = new();

    #region Execute - Zip file validation

    [Fact]
    public void Execute_ZipFileNotFound_ReturnsFalseAndLogsError()
    {
        // Arrange - file does not exist in MockFileSystem
        using ZipDeploy task = CreateTask();

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        _buildEngine.VerifyLog(
            LogLevel.Error,
            Strings.Deploy_ZipNotFound,
            TestZipPath);
    }

    [Fact]
    public void Execute_ZipFileExists_DoesNotLogZipNotFound()
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        using MockHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateStatusContent(DeployStatus.Success)
        });

        DeploymentClient client = CreateDeploymentClient(handler);
        using ZipDeploy task = CreateTask(client);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Execute - PublishUrl validation

    [Theory]
    [InlineData("not-an-url")]
    [InlineData("ftp://invalid.test/")]
    [InlineData("")]
    public void Execute_InvalidPublishUrl_ReturnsFalse(string invalidUrl)
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        using ZipDeploy task = CreateTask();
        task.PublishUrl = invalidUrl;

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        _buildEngine.VerifyLog(
            LogLevel.Error,
            Strings.Deploy_InvalidPublishUrl,
            invalidUrl);
    }

    [Theory]
    [InlineData("http://functionapp.scm.test/")]
    [InlineData("https://functionapp.scm.test/")]
    public void Execute_ValidHttpPublishUrl_DoesNotFailOnUrlParsing(string validUrl)
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        using MockHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateStatusContent(DeployStatus.Success)
        });

        DeploymentClient client = CreateDeploymentClient(handler);
        using ZipDeploy task = CreateTask(client);
        task.PublishUrl = validUrl;

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Execute - Successful deployments

    [Theory]
    [InlineData(DeployStatus.Success)]
    [InlineData(DeployStatus.PartialSuccess)]
    public void Execute_DeployReturnsSuccessStatus_ReturnsTrue(DeployStatus status)
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        using MockHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateStatusContent(status)
        });
        DeploymentClient client = CreateDeploymentClient(handler);
        using ZipDeploy task = CreateTask(client);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Execute - Failed deployments

    [Theory]
    [InlineData(DeployStatus.Failed)]
    [InlineData(DeployStatus.Conflict)]
    [InlineData(DeployStatus.Unknown)]
    public void Execute_DeployReturnsFailureStatus_ReturnsFalse(DeployStatus status)
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        Mock<DeploymentClient> mockClient = new(MockBehavior.Strict, new HttpClient(), (Microsoft.Build.Utilities.TaskLoggingHelper?)null);
        mockClient
            .Setup(c => c.ZipDeployAsync(It.IsAny<ZipDeployRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);
        using ZipDeploy task = CreateTask(mockClient.Object);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        _buildEngine.VerifyLog(
            LogLevel.Error,
            Strings.Deploy_CompletedFailure,
            TestZipPath,
            "https://functionapp.scm.test/api/zipdeploy?isAsync=true",
            status);
    }

    [Fact]
    public void Execute_DeploymentException_ReturnsFalseAndLogsError()
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        using MockHttpMessageHandler handler = new(_ =>
        {
            throw new DeploymentException("Deployment failed: internal error")
            {
                StatusCode = HttpStatusCode.InternalServerError,
                DeployStatus = DeployStatus.Failed,
            };
        });
        DeploymentClient client = CreateDeploymentClient(handler);
        using ZipDeploy task = CreateTask(client);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        _buildEngine.VerifyLog(
            LogLevel.Error,
            Strings.Deploy_Failed,
            "Deployment failed: internal error",
            "https://functionapp.scm.test/api/zipdeploy?isAsync=true",
            HttpStatusCode.InternalServerError,
            DeployStatus.Failed);
    }

    #endregion

    #region Execute - UseBlobContainerDeploy

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Execute_UseBlobContainerDeploy_SetsRequestProperty(bool useBlobContainer)
    {
        // Arrange
        Uri? capturedUri = null;
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        using MockHttpMessageHandler handler = new(req =>
        {
            capturedUri = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = CreateStatusContent(DeployStatus.Success)
            };
        });
        DeploymentClient client = CreateDeploymentClient(handler);
        using ZipDeploy task = CreateTask(client);
        task.UseBlobContainerDeploy = useBlobContainer;

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        capturedUri.Should().NotBeNull();

        if (useBlobContainer)
        {
            capturedUri!.PathAndQuery.Should().Contain("api/publish");
        }
        else
        {
            capturedUri!.PathAndQuery.Should().Contain("api/zipdeploy");
        }
    }

    #endregion

    #region Cancel

    [Fact]
    public void Execute_CancelledBeforeExecution_ThrowsCancellation()
    {
        // Arrange
        _fileSystem.AddFile(TestZipPath, new MockFileData("fake zip content"));
        Mock<DeploymentClient> mockClient = new(MockBehavior.Strict, new HttpClient(), (Microsoft.Build.Utilities.TaskLoggingHelper?)null);
        mockClient
            .Setup(c => c.ZipDeployAsync(It.IsAny<ZipDeployRequest>(), It.IsAny<CancellationToken>()))
            .Returns((ZipDeployRequest _, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(DeployStatus.Success);
            });
        using ZipDeploy task = CreateTask(mockClient.Object);

        // Act
        task.Cancel();
        Action act = () => task.Execute();

        // Assert - cancellation propagates as the task does not catch OperationCanceledException
        act.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        ZipDeploy task = CreateTask();

        // Act
        Action act = () => task.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleDispose_DoesNotThrow()
    {
        // Arrange
        ZipDeploy task = CreateTask();
        task.Dispose();

        // Act - dispose again
        Action act = () => task.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Property defaults

    [Fact]
    public void Properties_DefaultValues()
    {
        // Arrange & Act
        using ZipDeploy task = new(_fileSystem)
        {
            BuildEngine = _buildEngine.Object,
        };

        // Assert
        using (new AssertionScope())
        {
            task.ZipContentsPath.Should().BeEmpty();
            task.DeploymentUsername.Should().BeEmpty();
            task.DeploymentPassword.Should().BeEmpty();
            task.PublishUrl.Should().BeEmpty();
            task.DotnetSdkVersion.Should().Be("<unknown>");
            task.UseBlobContainerDeploy.Should().BeFalse();
        }
    }

    #endregion

    #region DotnetSdkVersion

    [Fact]
    public void BuildHttpClient_UserAgentIsSet()
    {
        // Arrange
        using ZipDeploy task = CreateTask();
        task.DotnetSdkVersion = "10.0.100";

        // Act
        HttpClient client = task.BuildHttpClient(new Uri(TestPublishUrl));

        // Assert
        client.DefaultRequestHeaders.UserAgent.Should().Contain(ZipDeploy.SdkUserAgentHeader);
        client.DefaultRequestHeaders.UserAgent.Should().Contain(ZipDeploy.OsUserAgentHeader);
        client.DefaultRequestHeaders.UserAgent.Should().Contain(new ProductInfoHeaderValue("Microsoft.NET.Sdk", "10.0.100"));
    }

    #endregion

    #region Helpers

    private static StringContent CreateStatusContent(DeployStatus status, string? text = null)
    {
        var payload = new
        {
            status = (int)status,
            status_text = text ?? string.Empty
        };
        string json = JsonSerializer.Serialize(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static DeploymentClient CreateDeploymentClient(MockHttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = new Uri(TestPublishUrl) };
        return new DeploymentClient(httpClient);
    }

    private ZipDeploy CreateTask(DeploymentClient? client = null)
    {
        return new(_fileSystem, client)
        {
            BuildEngine = _buildEngine.Object,
            ZipContentsPath = TestZipPath,
            PublishUrl = TestPublishUrl,
            DeploymentUsername = TestUsername,
            DeploymentPassword = TestPassword
        };
    }

    /// <summary>
    /// Mock HttpMessageHandler for testing HttpClient calls.
    /// </summary>
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        public MockHttpMessageHandler(HttpResponseMessage response)
            : this(_ => response)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_responseFactory(request));
        }
    }

    #endregion
}
