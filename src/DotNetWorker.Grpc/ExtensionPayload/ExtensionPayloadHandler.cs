// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.ExtensionPayloadCleanup;
using static Microsoft.Azure.Functions.Worker.Grpc.ExtensionPayloadLimits;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal sealed partial class ExtensionPayloadHandler(IExtensionPayloadProvider provider, IOptions<GrpcWorkerStartupOptions> startupOptions,
    ILogger<ExtensionPayloadHandler> logger) : IExtensionPayloadHandler
{
    private readonly IExtensionPayloadProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly GrpcWorkerStartupOptions _startupOptions = startupOptions?.Value ?? throw new ArgumentNullException(nameof(startupOptions));
    private readonly ILogger<ExtensionPayloadHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private Transfer? _activeTransfer;

    public Task SendAsync(string requestId, IWorkerClient client, CancellationToken stopping)
    {
        if (!client.SupportsExtensionPayload)
        {
            return RejectAsync(requestId, client, "Extension payload transfer is not supported by this worker transport.");
        }

        if (string.IsNullOrEmpty(requestId))
        {
            return RejectAsync(requestId, client, "An extension payload request must have a request_id.");
        }

        Transfer transfer = new(requestId, client.ConnectionClosed, stopping);
        Transfer? active = Interlocked.CompareExchange(ref _activeTransfer, transfer, null);
        if (active is not null)
        {
            transfer.Cancellation.Dispose();
            if (string.Equals(active.RequestId, requestId, StringComparison.Ordinal))
            {
                LogAlreadyActive(_logger, requestId);
                return active.Completion.Task;
            }

            return RejectAsync(requestId, client, "Another extension payload request is already active.");
        }

        // Claim ownership before dispatch so worker shutdown can cancel and drain the active transfer.
        _ = Task.Run(() => RunAsync(transfer, client));
        return transfer.Completion.Task;
    }

    public Task CancelAsync()
    {
        Transfer? transfer = Interlocked.CompareExchange(ref _activeTransfer, null, null);
        if (transfer is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            transfer.Cancellation.Cancel();
        }
        catch (ObjectDisposedException) when (!ReferenceEquals(Interlocked.CompareExchange(ref _activeTransfer, null, null), transfer))
        {
            // Only this finished generation may have disposed the captured cancellation source.
        }
        catch (Exception exception)
        {
            return DrainCancellationFailureAsync(transfer.Completion.Task, exception);
        }

        return transfer.Completion.Task;
    }

    private static async Task DrainCancellationFailureAsync(Task transfer, Exception cancellationFailure)
    {
        try
        {
            await transfer.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new AggregateException(cancellationFailure, exception);
        }

        ExceptionDispatchInfo.Capture(cancellationFailure).Throw();
    }

    private async Task RunAsync(Transfer transfer, IWorkerClient client)
    {
        Exception? failure = null;
        try
        {
            await StreamAsync(transfer, client).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        Interlocked.CompareExchange(ref _activeTransfer, null, transfer);
        failure = Dispose(transfer.Cancellation, failure);
        if (failure is null)
        {
            transfer.Completion.TrySetResult(true);
        }
        else
        {
            transfer.Completion.TrySetException(failure);
        }
    }

    private async Task StreamAsync(Transfer transfer, IWorkerClient client)
    {
        ulong offset = 0;
        Stream? payload = null;
        Exception? failure = null;
        try
        {
            int chunkSize = GetChunkSize(transfer.RequestId);
            transfer.Cancellation.Token.ThrowIfCancellationRequested();
            payload = await _provider.OpenReadAsync(transfer.Cancellation.Token).ConfigureAwait(false);
            byte[] buffer = new byte[chunkSize];
            int read;
            while ((read = await payload.ReadAsync(buffer, 0, buffer.Length, transfer.Cancellation.Token).ConfigureAwait(false)) != 0)
            {
                if (offset + (uint)read > (ulong)MaximumArchiveBytes)
                {
                    throw new InvalidDataException("The extension payload exceeds the 256 MiB ZIP limit.");
                }

                StreamingMessage message = new()
                {
                    RequestId = transfer.RequestId,
                    ExtensionPayloadResponse = new() { Offset = offset, Chunk = ByteString.CopyFrom(buffer, 0, read) }
                };
                await client.SendPayloadMessageAsync(message, transfer.Cancellation.Token).ConfigureAwait(false);
                offset += (uint)read;
            }

            transfer.Cancellation.Token.ThrowIfCancellationRequested();
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        failure = Dispose(payload, failure);
        if (failure is OperationCanceledException && transfer.Cancellation.IsCancellationRequested)
        {
            LogCancelled(_logger, transfer.RequestId);
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        StatusResult result = new() { Status = StatusResult.Types.Status.Success };
        if (failure is not null)
        {
            result.Status = StatusResult.Types.Status.Failure;
            result.Result = "Extension payload transfer failed. See the worker logs for details.";
            LogFailure(_logger, transfer.RequestId, failure);
            if (transfer.Cancellation.IsCancellationRequested)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        // Live operations send a terminal result; connection or worker shutdown cancels after cleanup.
        try
        {
            await client.SendPayloadMessageAsync(CreateTerminal(transfer.RequestId, offset, result), transfer.Cancellation.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (failure is not null)
        {
            throw new AggregateException(failure, exception);
        }
    }

    private Task RejectAsync(string requestId, IWorkerClient client, string reason)
    {
        LogRejected(_logger, requestId, reason);
        StreamingMessage message = CreateTerminal(requestId, 0, new() { Status = StatusResult.Types.Status.Failure, Result = reason });
        return client.SupportsExtensionPayload
            ? client.SendPayloadMessageAsync(message, client.ConnectionClosed).AsTask()
            : client.SendMessageAsync(message).AsTask();
    }

    private StreamingMessage CreateTerminal(string requestId, ulong offset, StatusResult result)
    {
        StreamingMessage message = new()
        {
            RequestId = requestId,
            ExtensionPayloadResponse = new() { Offset = offset, Result = result }
        };
        if (_startupOptions.GrpcMaxMessageLength > 0 && message.CalculateSize() > _startupOptions.GrpcMaxMessageLength)
        {
            result.Result = string.Empty;
            if (message.CalculateSize() > _startupOptions.GrpcMaxMessageLength)
            {
                throw new InvalidOperationException("The configured gRPC message limit cannot fit a correlated extension payload response.");
            }
        }

        return message;
    }

    private int GetChunkSize(string requestId)
    {
        if (_startupOptions.GrpcMaxMessageLength <= 0)
        {
            throw new InvalidOperationException("The configured gRPC message limit must be positive for extension payload transfer.");
        }

        StreamingMessage envelope = new()
        {
            RequestId = requestId,
            ExtensionPayloadResponse = new() { Offset = (ulong)MaximumArchiveBytes, Chunk = ByteString.CopyFrom(new byte[MaximumChunkBytes]) }
        };
        int overhead = envelope.CalculateSize() - MaximumChunkBytes;
        int size = Math.Min(MaximumChunkBytes, _startupOptions.GrpcMaxMessageLength - overhead);
        if (size <= 0)
        {
            throw new InvalidOperationException("The configured gRPC message limit cannot fit an extension payload chunk.");
        }

        return size;
    }

    private sealed class Transfer(string requestId, CancellationToken connectionClosed, CancellationToken stopping)
    {
        public string RequestId { get; } = requestId;
        public CancellationTokenSource Cancellation { get; } = CancellationTokenSource.CreateLinkedTokenSource(connectionClosed, stopping);
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [LoggerMessage(1, LogLevel.Error, "Extension payload request {RequestId} failed.")]
    private static partial void LogFailure(ILogger logger, string requestId, Exception exception);

    [LoggerMessage(2, LogLevel.Warning, "Extension payload request {RequestId} was rejected: {Reason}")]
    private static partial void LogRejected(ILogger logger, string requestId, string reason);

    [LoggerMessage(3, LogLevel.Debug, "Extension payload transfer {RequestId} stopped during worker or connection shutdown.")]
    private static partial void LogCancelled(ILogger logger, string requestId);

    [LoggerMessage(4, LogLevel.Debug, "Extension payload request {RequestId} is already active; its original response will complete the operation.")]
    private static partial void LogAlreadyActive(ILogger logger, string requestId);
}
