// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using Logger = Microsoft.Build.Utilities.TaskLoggingHelper;

namespace Azure.Functions.Sdk.ZipDeploy;

public partial class DeploymentClient(HttpClient client, Logger? logger = null)
{
    private const int RetryCount = 3;
    private static readonly TimeSpan StatusRefreshDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly Logger? _logger = logger;
    private readonly HttpClient _client = client;

    public virtual async Task<DeployStatus> ZipDeployAsync(ZipDeployRequest request, CancellationToken cancellation)
    {
        Throw.IfNull(request);
        using HttpRequestMessage httpRequest = request.CreateRequestMessage();
        using HttpResponseMessage response = await _client.SendAsync(httpRequest, cancellation);
        StatusResult status = await StatusResult.ParseAsync(response, cancellation);

        if (response.Headers.Location is Uri location)
        {
            // If the response contains a location header, we can assume the deployment is asynchronous
            _logger?.LogMessageFromResources(nameof(Strings.Deploy_AsyncDeployment), location);
            return await PollDeploymentStatusAsync(location, cancellation);
        }

        return DeployStatus.Success;
    }

    private async Task<DeployStatus> PollDeploymentStatusAsync(Uri location, CancellationToken cancellation)
    {
        while (true)
        {
            await Task.Delay(StatusRefreshDelay, cancellation);
            StatusResult status = await GetDeploymentStatusAsync(location, cancellation);
            if (status.IsCompleted)
            {
                return status.Status;
            }

            if (!string.IsNullOrEmpty(status.Text))
            {
                _logger?.LogMessageFromResources(
                    MessageImportance.Low, nameof(Strings.Deploy_StatusWithText), status.Status, status.Text);
            }
            else
            {
                _logger?.LogMessageFromResources(MessageImportance.Low, nameof(Strings.Deploy_Status), status.Status);
            }
        }
    }

    private async Task<StatusResult> GetDeploymentStatusAsync(Uri location, CancellationToken cancellation)
    {
        int retry = 0;
        while (true)
        {
            try
            {
                using HttpResponseMessage response = await _client.GetAsync(location, cancellation);
                return await StatusResult.ParseAsync(response, cancellation);
            }
            catch (DeploymentException ex)
            {
                if (retry++ == RetryCount)
                {
                    throw;
                }

                _logger?.LogWarningFromResources(
                    nameof(Strings.Deploy_PollFailure),
                    location,
                    ex.StatusCode,
                    retry + 1,
                    RetryCount);
                await Task.Delay(RetryDelay, cancellation);
            }
        }
    }

    public class StatusResult
    {
        public static readonly StatusResult Default = new();

        [JsonPropertyName("status")]
        public DeployStatus Status { get; set; } = DeployStatus.Unknown;

        [JsonPropertyName("status_text")]
        public string Text { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsCompleted => Status is
            DeployStatus.Success or
            DeployStatus.Failed or
            DeployStatus.PartialSuccess or
            DeployStatus.Conflict or
            DeployStatus.Unknown;

        public static async Task<StatusResult> ParseAsync(HttpResponseMessage response, CancellationToken cancellation)
        {
            StatusResult result = await ParseResponseAsync(response, cancellation);

            try
            {
                response.EnsureSuccessStatusCode();
                return result;
            }
            catch (HttpRequestException ex)
            {
                throw result.ThrowFailure(response.StatusCode, ex);
            }
        }

        [DoesNotReturn]
        public Exception ThrowFailure(HttpStatusCode statusCode, Exception inner)
        {
            string message = !string.IsNullOrEmpty(Text)
                ? $"Deployment failed: {Text}"
                : "Deployment failed with an unknown error";
            throw new DeploymentException(message, inner)
            {
                DeployStatus = Status,
                StatusCode = statusCode,
            };
        }

        private static async Task<StatusResult> ParseResponseAsync(
            HttpResponseMessage response, CancellationToken cancellation)
        {
            if (response.Content is null)
            {
                return Default;
            }

            using Stream stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<StatusResult>(stream, cancellationToken: cancellation)
                ?? Default;
        }
    }
}
