// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Specialized;
using Microsoft.Build.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Azure.Functions.Sdk.ZipDeploy.Tests;

public sealed class DeploymentClientTests
{
    private static readonly Uri TestBaseUri = new("https://functionapp.scm.test/");
    private static readonly Uri TestLocationUri = new("https://functionapp.scm.test/api/deployments/latest");

    #region ZipDeployAsync Tests

    [Fact]
    public async Task ZipDeployAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        using HttpClient httpClient = new();
        DeploymentClient client = new(httpClient);

        // Act
        Func<Task> act = () => client.ZipDeployAsync(null!, default);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .WithParameterName("request");
    }

    [Fact]
    public async Task ZipDeployAsync_SuccessWithoutLocationHeader_ReturnsSuccess()
    {
        // Arrange
        using MockHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateStatusContent(DeployStatus.Success)
        });

        using Stream content = new MemoryStream();
        DeploymentClient client = CreateDeploymentClient(handler, null);
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        DeployStatus result = await client.ZipDeployAsync(request, default);

        // Assert
        result.Should().Be(DeployStatus.Success);
    }

    [Theory]
    [InlineData(DeployStatus.Success)]
    [InlineData(DeployStatus.Failed)]
    [InlineData(DeployStatus.PartialSuccess)]
    [InlineData(DeployStatus.Conflict)]
    public async Task ZipDeployAsync_WithLocationHeader_PollsAndReturnsStatus(DeployStatus expectedStatus)
    {
        // Arrange
        int pollCount = 0;
        using MockHttpMessageHandler handler = new(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return CreateResponseWithLocation(
                    HttpStatusCode.Accepted,
                    TestLocationUri,
                    CreateStatusContent(DeployStatus.Pending));
            }

            // Simulate polling - return expected status on first poll
            pollCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = CreateStatusContent(expectedStatus)
            };
        });

        DeploymentClient client = CreateDeploymentClient(handler, null);
        using Stream content = new MemoryStream();
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        DeployStatus result = await client.ZipDeployAsync(request, default);

        // Assert
        result.Should().Be(expectedStatus);
        pollCount.Should().BePositive("Should have polled at least once");
    }

    [Fact]
    public async Task ZipDeployAsync_WithLocationHeader_PollsUntilCompleted()
    {
        // Arrange
        int pollCount = 0;
        using MockHttpMessageHandler handler = new(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return CreateResponseWithLocation(
                    HttpStatusCode.Accepted,
                    TestLocationUri,
                    CreateStatusContent(DeployStatus.Pending));
            }

            pollCount++;
            // Return non-completed status for first 2 polls, then completed
            DeployStatus status = pollCount < 3 ? DeployStatus.Deploying : DeployStatus.Success;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = CreateStatusContent(status)
            };
        });

        DeploymentClient client = CreateDeploymentClient(handler, null);
        using Stream content = new MemoryStream();
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        DeployStatus result = await client.ZipDeployAsync(request, default);

        // Assert
        result.Should().Be(DeployStatus.Success);
        pollCount.Should().Be(3, "Should have polled 3 times before completion");
    }

    [Fact]
    public async Task ZipDeployAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        using MockHttpMessageHandler handler = new(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return CreateResponseWithLocation(
                    HttpStatusCode.Accepted,
                    TestLocationUri,
                    CreateStatusContent(DeployStatus.Building));
            }

            // Cancel during poll
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        });

        DeploymentClient client = CreateDeploymentClient(handler, null);
        using Stream content = new MemoryStream();
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        Func<Task> act = () => client.ZipDeployAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ZipDeployAsync_HttpError_ThrowsDeploymentException()
    {
        // Arrange
        using MockHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = CreateStatusContent(DeployStatus.Failed, "Server error")
        });

        using HttpClient httpClient = new(handler) { BaseAddress = TestBaseUri };
        DeploymentClient client = new(httpClient);
        using Stream content = new MemoryStream();
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        Func<Task> act = () => client.ZipDeployAsync(request, default);

        // Assert
        ExceptionAssertions<DeploymentException> ex = await act.Should().ThrowAsync<DeploymentException>();
        ex.WithMessage("*Server error*")
          .Where(e => e.StatusCode == HttpStatusCode.InternalServerError)
          .Where(e => e.DeployStatus == DeployStatus.Failed);
    }

    #endregion

    #region StatusResult Tests

    [Theory]
    [InlineData(DeployStatus.Success, true)]
    [InlineData(DeployStatus.Failed, true)]
    [InlineData(DeployStatus.PartialSuccess, true)]
    [InlineData(DeployStatus.Conflict, true)]
    [InlineData(DeployStatus.Unknown, true)]
    [InlineData(DeployStatus.Pending, false)]
    [InlineData(DeployStatus.Building, false)]
    [InlineData(DeployStatus.Deploying, false)]
    public void StatusResult_IsCompleted_ReturnsCorrectValue(DeployStatus status, bool expectedIsCompleted)
    {
        // Arrange
        DeploymentClient.StatusResult result = new() { Status = status };

        // Act & Assert
        result.IsCompleted.Should().Be(expectedIsCompleted);
    }

    [Fact]
    public void StatusResult_Default_HasExpectedValues()
    {
        // Arrange & Act
        DeploymentClient.StatusResult result = DeploymentClient.StatusResult.Default;

        // Assert
        using (new AssertionScope())
        {
            result.Status.Should().Be(DeployStatus.Unknown);
            result.Text.Should().BeEmpty();
            result.IsCompleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task StatusResult_ParseAsync_SuccessfulResponse_ReturnsStatusResult()
    {
        // Arrange
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = CreateStatusContent(DeployStatus.Success, "Deployment succeeded")
        };

        // Act
        DeploymentClient.StatusResult result = await DeploymentClient.StatusResult.ParseAsync(
            response, default);

        // Assert
        using (new AssertionScope())
        {
            result.Status.Should().Be(DeployStatus.Success);
            result.Text.Should().Be("Deployment succeeded");
        }
    }

    [Fact]
    public async Task StatusResult_ParseAsync_FailedResponse_ThrowsDeploymentException()
    {
        // Arrange
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest)
        {
            Content = CreateStatusContent(DeployStatus.Failed, "Bad request")
        };

        // Act
        Func<Task> act = () => DeploymentClient.StatusResult.ParseAsync(response, default);

        // Assert
        ExceptionAssertions<DeploymentException> ex = await act.Should().ThrowExactlyAsync<DeploymentException>();
            ex.WithMessage("*Bad request*")
              .Where(e => e.StatusCode == HttpStatusCode.BadRequest)
              .Where(e => e.DeployStatus == DeployStatus.Failed)
              .WithInnerException<HttpRequestException>();
    }

    [Fact]
    public async Task StatusResult_ParseAsync_EmptyJsonObject_ReturnsDefaultValues()
    {
        // Arrange
        string emptyJson = "{}";
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(emptyJson, Encoding.UTF8, "application/json")
        };

        // Act
        DeploymentClient.StatusResult result = await DeploymentClient.StatusResult.ParseAsync(
            response, default);

        // Assert
        using (new AssertionScope())
        {
            result.Status.Should().Be(DeployStatus.Unknown);
            result.Text.Should().BeEmpty();
        }
    }

    [Fact]
    public void StatusResult_ThrowFailure_WithText_ThrowsWithMessage()
    {
        // Arrange
        DeploymentClient.StatusResult result = new()
        {
            Status = DeployStatus.Failed,
            Text = "Custom error message"
        };

        HttpRequestException inner = new("Inner exception");

        // Act
        Action act = () => result.ThrowFailure(HttpStatusCode.InternalServerError, inner);

        // Assert
        act.Should().Throw<DeploymentException>()
            .WithMessage("*Custom error message*")
            .Where(e => e.StatusCode == HttpStatusCode.InternalServerError)
            .Where(e => e.DeployStatus == DeployStatus.Failed)
            .Where(e => e.InnerException == inner);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void StatusResult_ThrowFailure_WithoutText_ThrowsGenericMessage(string? text)
    {
        // Arrange
        DeploymentClient.StatusResult result = new()
        {
            Status = DeployStatus.Unknown,
            Text = text ?? string.Empty
        };
        HttpRequestException inner = new("Inner exception");

        // Act
        Action act = () => result.ThrowFailure(HttpStatusCode.BadGateway, inner);

        // Assert
        act.Should().Throw<DeploymentException>()
            .WithMessage("*unknown error*")
            .Where(e => e.StatusCode == HttpStatusCode.BadGateway)
            .Where(e => e.DeployStatus == DeployStatus.Unknown)
            .Where(e => e.InnerException == inner);
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task ZipDeployAsync_PollFailure_RetriesUpToMaxRetries()
    {
        // Arrange
        int pollAttempts = 0;
        using MockHttpMessageHandler handler = new(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return CreateResponseWithLocation(
                    HttpStatusCode.Accepted,
                    TestLocationUri,
                    CreateStatusContent(DeployStatus.Building));
            }

            pollAttempts++;
            // Fail first 3 attempts, succeed on 4th
            if (pollAttempts <= 3)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = CreateStatusContent(DeployStatus.Unknown, "Service unavailable")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = CreateStatusContent(DeployStatus.Success)
            };
        });

        using Stream content = new MemoryStream();
        DeploymentClient client = CreateDeploymentClient(handler, null);
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        DeployStatus result = await client.ZipDeployAsync(request, default);

        // Assert
        result.Should().Be(DeployStatus.Success);
        pollAttempts.Should().Be(4, "Should have attempted 4 times (3 retries + 1 success)");
    }

    [Fact]
    public async Task ZipDeployAsync_PollFailure_ExceedsMaxRetries_ThrowsDeploymentException()
    {
        // Arrange
        int pollAttempts = 0;
        using MockHttpMessageHandler handler = new(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return CreateResponseWithLocation(
                    HttpStatusCode.Accepted,
                    TestLocationUri,
                    CreateStatusContent(DeployStatus.Building));
            }

            pollAttempts++;
            // Always fail
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = CreateStatusContent(DeployStatus.Unknown, "Service unavailable")
            };
        });

        using Stream content = new MemoryStream();
        DeploymentClient client = CreateDeploymentClient(handler, null);
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        Func<Task> act = () => client.ZipDeployAsync(request, default);

        // Assert
        (await act.Should().ThrowAsync<DeploymentException>())
            .WithMessage("*Service unavailable*")
            .Where(e => e.StatusCode == HttpStatusCode.ServiceUnavailable)
            .Where(e => e.DeployStatus == DeployStatus.Unknown);
        pollAttempts.Should().Be(4, "Should have attempted 4 times (initial + 3 retries)");
    }

    #endregion

    #region Logger Integration Tests

    [Fact]
    public async Task ZipDeployAsync_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        using MockHttpMessageHandler handler = new(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return CreateResponseWithLocation(
                    HttpStatusCode.Accepted,
                    TestLocationUri,
                    CreateStatusContent(DeployStatus.Building));
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = CreateStatusContent(DeployStatus.Success)
            };
        });

        using Stream content = new MemoryStream();

        // Pass null logger to ensure no logging issues
        DeploymentClient client = CreateDeploymentClient(handler, null);
        ZipDeployRequest request = new("User", "Pass", content);

        // Act
        DeployStatus result = await client.ZipDeployAsync(request, default);

        // Assert - no exception and correct result
        result.Should().Be(DeployStatus.Success);
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

    private static HttpResponseMessage CreateResponseWithLocation(
        HttpStatusCode statusCode,
        Uri location,
        HttpContent? content = null)
    {
        HttpResponseMessage response = new(statusCode)
        {
            Content = content
        };
        response.Headers.Location = location;
        return response;
    }

    private static DeploymentClient CreateDeploymentClient(
        MockHttpMessageHandler handler, TaskLoggingHelper? logger = null)
    {
        HttpClient httpClient = new(handler) { BaseAddress = TestBaseUri };
        return new DeploymentClient(httpClient, logger);
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
