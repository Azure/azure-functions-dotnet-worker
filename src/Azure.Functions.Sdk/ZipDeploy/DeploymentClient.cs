// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
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
    private readonly HttpClient _client = Throw.IfNull(client);

    public virtual async Task<DeployStatus> ZipDeployAsync(ZipDeployRequest request, CancellationToken cancellation)
    {
        Throw.IfNull(request);
        Uri? location;
        AuthenticationHeaderValue? authorization;
        StatusResult status;
        using (HttpRequestMessage httpRequest = request.CreateRequestMessage())
        using (HttpResponseMessage response = await _client.SendAsync(httpRequest, cancellation))
        {
            status = await StatusResult.ParseAsync(response, cancellation);
            location = response.Headers.Location;
            authorization = httpRequest.Headers.Authorization;
        }

        if (location is not null)
        {
            // If the response contains a location header, we can assume the deployment is asynchronous
            _logger?.LogMessageFromResources(nameof(Strings.Deploy_AsyncDeployment), location.ToLogSafeString());
            return await PollDeploymentStatusAsync(location, authorization, cancellation);
        }

        return status.Status;
    }

    private async Task<DeployStatus> PollDeploymentStatusAsync(
        Uri location, AuthenticationHeaderValue? authorization, CancellationToken cancellation)
    {
        while (true)
        {
            await Task.Delay(StatusRefreshDelay, cancellation);
            StatusResult status = await GetDeploymentStatusAsync(location, authorization, cancellation);
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

    private async Task<StatusResult> GetDeploymentStatusAsync(
        Uri location, AuthenticationHeaderValue? authorization, CancellationToken cancellation)
    {
        int retry = 0;
        while (true)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, location);
                request.Headers.Authorization = authorization;
                using HttpResponseMessage response = await _client.SendAsync(request, cancellation);
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
                    location.ToLogSafeString(),
                    ex.StatusCode,
                    retry,
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
            StatusResult result;
            try
            {
                result = await ParseResponseAsync(response, cancellation);
            }
            catch (JsonException) when (!response.IsSuccessStatusCode)
            {
                result = Default;
            }

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
            if (response.Content is null || response.Content.Headers.ContentLength is 0)
            {
                return Default;
            }

            using Stream content = await response.Content.ReadAsStreamAsync();

            // First test if there is any content at all.
            byte[] first = new byte[1];
            if (await content.ReadAsync(first, 0, 1, cancellation) == 0)
            {
                return Default;
            }

            using PrefixedStream stream = new(first[0], content);
            return await JsonSerializer.DeserializeAsync<StatusResult>(stream, cancellationToken: cancellation)
                ?? Default;
        }
    }

    /// <summary>
    /// A read-only stream that yields a single already-consumed byte before reading from the inner stream.
    /// </summary>
    private sealed class PrefixedStream(byte prefix, Stream inner) : Stream
    {
        private bool _prefixRead;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (TryReadPrefix(buffer, offset, count))
            {
                return 1;
            }

            return inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryReadPrefix(buffer, offset, count))
            {
                return Task.FromResult(1);
            }

            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private bool TryReadPrefix(byte[] buffer, int offset, int count)
        {
            if (_prefixRead || count == 0)
            {
                return false;
            }

            buffer[offset] = prefix;
            _prefixRead = true;
            return true;
        }
    }
}
