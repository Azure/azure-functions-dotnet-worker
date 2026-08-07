// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Testing.Protocol;

internal sealed class InMemoryFunctionsHost : IAsyncDisposable
{
    internal const string TestWorkerId = "testing-worker";

    private readonly object _stateLock = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<StreamingMessage>> _pendingRequests = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _activeInvocations = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<RpcLog> _logs = new();
    private readonly ConcurrentQueue<ProtocolTranscriptEntry> _transcript = new();
    private readonly TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TimeSpan _shutdownTimeout;
    private readonly int _maximumMessageLength;
    private Func<StreamingMessage, Task>? _sendToWorker;
    private string? _functionAppDirectory;
    private IReadOnlyList<RpcFunctionMetadata> _functionMetadata = Array.Empty<RpcFunctionMetadata>();
    private InMemoryFunctionsHostState _state;

    internal InMemoryFunctionsHost(TimeSpan shutdownTimeout, int maximumMessageLength)
    {
        if (shutdownTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), "The shutdown timeout must be positive.");
        }

        if (maximumMessageLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMessageLength), "The maximum message length must be positive.");
        }

        _shutdownTimeout = shutdownTimeout;
        _maximumMessageLength = maximumMessageLength;
    }

    internal InMemoryFunctionsHostState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    internal IReadOnlyList<RpcFunctionMetadata> FunctionMetadata => _functionMetadata;

    internal IReadOnlyCollection<RpcLog> Logs => new ReadOnlyCollection<RpcLog>(_logs.ToArray());

    internal IReadOnlyList<ProtocolTranscriptEntry> Transcript
        => Array.AsReadOnly(_transcript.ToArray());

    internal void Connect(IMessageProcessor messageProcessor)
    {
        ArgumentNullException.ThrowIfNull(messageProcessor);
        Connect(messageProcessor.ProcessMessageAsync);
    }

    internal void Connect(Func<StreamingMessage, Task> sendToWorker)
    {
        ArgumentNullException.ThrowIfNull(sendToWorker);

        lock (_stateLock)
        {
            EnsureState(InMemoryFunctionsHostState.Created);
            _sendToWorker = sendToWorker;
            _state = InMemoryFunctionsHostState.Connected;
        }

        _connected.TrySetResult();
    }

    internal async Task InitializeAsync(string functionAppDirectory, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(functionAppDirectory))
        {
            throw new ArgumentException("A function application directory is required.", nameof(functionAppDirectory));
        }

        ValidateTimeout(timeout);
        try
        {
            await _connected.Task.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"The worker did not connect within {timeout}.");
        }

        _functionAppDirectory = functionAppDirectory;
        Transition(InMemoryFunctionsHostState.Connected, InMemoryFunctionsHostState.Initializing);

        try
        {
            StreamingMessage init = await RequestAsync(
                new StreamingMessage
                {
                    WorkerInitRequest = new WorkerInitRequest
                    {
                        HostVersion = "in-memory-testing",
                        FunctionAppDirectory = functionAppDirectory,
                        WorkerDirectory = functionAppDirectory
                    }
                },
                timeout,
                cancellationToken);
            EnsureSucceeded(init.WorkerInitResponse?.Result, "worker initialization");

            Transition(InMemoryFunctionsHostState.Initializing, InMemoryFunctionsHostState.LoadingMetadata);
            StreamingMessage metadata = await RequestAsync(
                new StreamingMessage
                {
                    FunctionsMetadataRequest = new FunctionsMetadataRequest
                    {
                        FunctionAppDirectory = functionAppDirectory
                    }
                },
                timeout,
                cancellationToken);
            FunctionMetadataResponse metadataResponse = metadata.FunctionMetadataResponse
                ?? throw new InvalidOperationException("The worker returned no function metadata response.");
            EnsureSucceeded(metadataResponse.Result, "function metadata discovery");

            RpcFunctionMetadata[] functions = metadataResponse.FunctionMetadataResults
                .Select(static item => item.Clone())
                .ToArray();
            ValidateMetadata(functions);
            _functionMetadata = Array.AsReadOnly(functions);

            Transition(InMemoryFunctionsHostState.LoadingMetadata, InMemoryFunctionsHostState.LoadingFunctions);
            foreach (RpcFunctionMetadata function in functions)
            {
                StreamingMessage load = await RequestAsync(
                    new StreamingMessage
                    {
                        FunctionLoadRequest = new FunctionLoadRequest
                        {
                            FunctionId = function.FunctionId,
                            Metadata = function.Clone(),
                            ManagedDependencyEnabled = function.ManagedDependencyEnabled
                        }
                    },
                    timeout,
                    cancellationToken);
                FunctionLoadResponse loadResponse = load.FunctionLoadResponse
                    ?? throw new InvalidOperationException($"The worker returned no load response for function '{function.Name}'.");
                EnsureSucceeded(loadResponse.Result, $"loading function '{function.Name}'");

                if (!string.Equals(loadResponse.FunctionId, function.FunctionId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The worker load response identified function '{loadResponse.FunctionId}' instead of '{function.FunctionId}'.");
                }
            }

            Transition(InMemoryFunctionsHostState.LoadingFunctions, InMemoryFunctionsHostState.Ready);
        }
        catch
        {
            SetFaulted();
            throw;
        }
    }

    internal async Task<InvocationResponse> InvokeAsync(
        InvocationRequest invocation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ValidateTimeout(timeout);
        EnsureReady();

        if (string.IsNullOrWhiteSpace(invocation.InvocationId))
        {
            throw new ArgumentException("An invocation ID is required.", nameof(invocation));
        }

        if (string.IsNullOrWhiteSpace(invocation.FunctionId))
        {
            throw new ArgumentException("A function ID is required.", nameof(invocation));
        }

        if (!_functionMetadata.Any(metadata => string.Equals(metadata.FunctionId, invocation.FunctionId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Function ID '{invocation.FunctionId}' was not loaded by this session.");
        }

        if (!_activeInvocations.TryAdd(invocation.InvocationId, 0))
        {
            throw new InvalidOperationException($"Invocation ID '{invocation.InvocationId}' is already active.");
        }

        try
        {
            StreamingMessage message = await RequestAsync(
                new StreamingMessage { InvocationRequest = invocation },
                timeout,
                cancellationToken);
            InvocationResponse response = message.InvocationResponse
                ?? throw new InvalidOperationException("The worker returned no invocation response.");

            if (!string.Equals(response.InvocationId, invocation.InvocationId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The worker response identified invocation '{response.InvocationId}' instead of '{invocation.InvocationId}'.");
            }

            return response;
        }
        finally
        {
            _activeInvocations.TryRemove(invocation.InvocationId, out _);
        }
    }

    internal async Task CancelInvocationAsync(
        string invocationId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invocationId))
        {
            throw new ArgumentException("An invocation ID is required.", nameof(invocationId));
        }

        ValidateTimeout(timeout);
        EnsureReady();

        if (!_activeInvocations.ContainsKey(invocationId))
        {
            return;
        }

        await RequestAsync(
            new StreamingMessage
            {
                InvocationCancel = new InvocationCancel { InvocationId = invocationId }
            },
            timeout,
            cancellationToken);
    }

    internal ValueTask AcceptWorkerMessageAsync(StreamingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _transcript.Enqueue(new ProtocolTranscriptEntry(false, message.Clone()));

        if (message.CalculateSize() > _maximumMessageLength)
        {
            SetFaulted();
            FaultPending(new InvalidOperationException(
                $"Worker message length {message.CalculateSize()} exceeds the configured maximum of {_maximumMessageLength} bytes."));
            return ValueTask.CompletedTask;
        }

        if (message.RpcLog is not null)
        {
            _logs.Enqueue(message.RpcLog.Clone());
            return ValueTask.CompletedTask;
        }

        if (!string.IsNullOrEmpty(message.RequestId)
            && _pendingRequests.TryRemove(message.RequestId, out TaskCompletionSource<StreamingMessage>? completion))
        {
            completion.TrySetResult(message);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        InMemoryFunctionsHostState prior;
        lock (_stateLock)
        {
            prior = _state;
            if (prior is InMemoryFunctionsHostState.Stopped or InMemoryFunctionsHostState.Stopping)
            {
                return;
            }

            _state = InMemoryFunctionsHostState.Stopping;
        }

        if (prior is not InMemoryFunctionsHostState.Created and not InMemoryFunctionsHostState.Faulted)
        {
            try
            {
                await RequestAsync(
                    new StreamingMessage { WorkerTerminate = new WorkerTerminate() },
                    _shutdownTimeout,
                    CancellationToken.None,
                    allowStopping: true);
            }
            catch (Exception exception) when (exception is TimeoutException or OperationCanceledException or InvalidOperationException)
            {
            }
        }

        FaultPending(new ObjectDisposedException(nameof(InMemoryFunctionsHost)));
        _activeInvocations.Clear();

        lock (_stateLock)
        {
            _state = InMemoryFunctionsHostState.Stopped;
        }
    }

    private async Task<StreamingMessage> RequestAsync(
        StreamingMessage request,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowStopping = false)
    {
        Func<StreamingMessage, Task> sendToWorker;
        lock (_stateLock)
        {
            if (_sendToWorker is null || (_state == InMemoryFunctionsHostState.Stopping && !allowStopping))
            {
                throw new InvalidOperationException($"The in-memory worker session cannot send messages while in state '{_state}'.");
            }

            sendToWorker = _sendToWorker;
        }

        request.RequestId = Guid.NewGuid().ToString("N");
        _transcript.Enqueue(new ProtocolTranscriptEntry(true, request.Clone()));
        int messageLength = request.CalculateSize();
        if (messageLength > _maximumMessageLength)
        {
            throw new InvalidOperationException(
                $"Host message length {messageLength} exceeds the configured maximum of {_maximumMessageLength} bytes.");
        }

        var completion = new TaskCompletionSource<StreamingMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(request.RequestId, completion))
        {
            throw new InvalidOperationException($"Request ID '{request.RequestId}' is already active.");
        }

        try
        {
            using IDisposable? directoryScope = _functionAppDirectory is null
                ? null
                : WorkerApplicationDirectoryContext.Push(_functionAppDirectory);
            await sendToWorker(request);
            return await completion.Task.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"The worker did not respond to '{request.ContentCase}' within {timeout}.");
        }
        finally
        {
            _pendingRequests.TryRemove(request.RequestId, out _);
        }
    }

    private static void ValidateMetadata(IEnumerable<RpcFunctionMetadata> functions)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (RpcFunctionMetadata function in functions)
        {
            if (string.IsNullOrWhiteSpace(function.FunctionId) || string.IsNullOrWhiteSpace(function.Name))
            {
                throw new InvalidOperationException("Worker metadata must include non-empty function IDs and names.");
            }

            if (!ids.Add(function.FunctionId))
            {
                throw new InvalidOperationException($"Worker metadata contains duplicate function ID '{function.FunctionId}'.");
            }

            if (!names.Add(function.Name))
            {
                throw new InvalidOperationException($"Worker metadata contains duplicate function name '{function.Name}'.");
            }
        }
    }

    private static void EnsureSucceeded(StatusResult? result, string operation)
    {
        if (result?.Status != StatusResult.Types.Status.Success)
        {
            string detail = result?.Exception?.Message ?? result?.Result ?? "No status result was returned.";
            throw new InvalidOperationException($"The worker failed during {operation}: {detail}");
        }
    }

    private void EnsureReady()
    {
        lock (_stateLock)
        {
            EnsureState(InMemoryFunctionsHostState.Ready);
        }
    }

    private void Transition(InMemoryFunctionsHostState expected, InMemoryFunctionsHostState next)
    {
        lock (_stateLock)
        {
            EnsureState(expected);
            _state = next;
        }
    }

    private void EnsureState(InMemoryFunctionsHostState expected)
    {
        if (_state != expected)
        {
            throw new InvalidOperationException($"Expected in-memory worker state '{expected}', but the current state is '{_state}'.");
        }
    }

    private void SetFaulted()
    {
        lock (_stateLock)
        {
            _state = InMemoryFunctionsHostState.Faulted;
        }
    }

    private static void ValidateTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The timeout must be positive.");
        }
    }

    private void FaultPending(Exception exception)
    {
        foreach ((string requestId, TaskCompletionSource<StreamingMessage> completion) in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(requestId, out _))
            {
                completion.TrySetException(exception);
            }
        }
    }
}

internal sealed record ProtocolTranscriptEntry(bool HostToWorker, StreamingMessage Message);
