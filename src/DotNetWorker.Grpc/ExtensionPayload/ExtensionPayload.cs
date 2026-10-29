// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Grpc;

/// <summary>
/// Creates filtered extension archives on demand and streams one active transfer over the worker transport.
/// </summary>
internal sealed partial class ExtensionPayload(IOptions<GrpcWorkerStartupOptions> startupOptions, ILogger<ExtensionPayload> logger)
{
    internal const int MaximumChunkBytes = 64 * 1024;
    internal const long MaximumArchiveBytes = 256L * 1024 * 1024;
    internal const long MaximumContentBytes = 1024L * 1024 * 1024;
    internal const int MaximumManifestBytes = 1024 * 1024;
    internal const int MaximumEntries = 10000;
    internal const int MaximumPathDepth = 32;
    internal const string ApplicationDirectoryVariable = WorkerApplicationDirectory.EnvironmentVariable;
    internal const string TemporaryFilePrefix = "functions-extension-payload-";

    private const string MetadataLoaderAssemblyName = "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader";
    private const string MetadataLoaderStartupTypeName = MetadataLoaderAssemblyName + ".Startup";

    private readonly GrpcWorkerStartupOptions _startupOptions = startupOptions?.Value ?? throw new ArgumentNullException(nameof(startupOptions));
    private readonly ILogger<ExtensionPayload> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private Transfer? _activeTransfer;

    /// <summary>
    /// Claims a transfer before dispatching it, reuses an identical active request, and rejects competing requests.
    /// The returned task completes after sending and cleanup finish.
    /// </summary>
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

    /// <summary>
    /// Requests shutdown cancellation and returns the active transfer's completion task so its owner can drain it.
    /// </summary>
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

        return transfer.Completion.Task;
    }

    /// <summary>
    /// Runs a claimed transfer and completes its shared task after releasing that transfer's ownership and cancellation state.
    /// </summary>
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

    /// <summary>
    /// Builds the ZIP and awaits each chunk write before advancing its offset.
    /// Deletes the archive before reporting success or failure; shutdown cancellation propagates after cleanup.
    /// </summary>
    private async Task StreamAsync(Transfer transfer, IWorkerClient client)
    {
        ulong offset = 0;
        FileStream? archive = null;
        Exception? failure = null;
        try
        {
            int chunkSize = GetChunkSize(transfer.RequestId);
            archive = await CreateArchiveAsync(transfer.Cancellation.Token).ConfigureAwait(false);
            byte[] buffer = new byte[chunkSize];
            int read;
            while ((read = await archive.ReadAsync(buffer, 0, buffer.Length, transfer.Cancellation.Token).ConfigureAwait(false)) != 0)
            {
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

        failure = Dispose(archive, failure);
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
        }

        // Live operations send a terminal result; connection or worker shutdown cancels after cleanup.
        await client.SendPayloadMessageAsync(CreateTerminal(transfer.RequestId, offset, result), transfer.Cancellation.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs a rejected request and sends a correlated failure through the available transport without creating a ZIP.
    /// </summary>
    private Task RejectAsync(string requestId, IWorkerClient client, string reason)
    {
        LogRejected(_logger, requestId, reason);
        StreamingMessage message = CreateTerminal(requestId, 0, new() { Status = StatusResult.Types.Status.Failure, Result = reason });
        return client.SupportsExtensionPayload
            ? client.SendPayloadMessageAsync(message, client.ConnectionClosed).AsTask()
            : client.SendMessageAsync(message).AsTask();
    }

    /// <summary>
    /// Builds the final correlated response, omitting diagnostic text if necessary to fit the configured message limit.
    /// </summary>
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

    /// <summary>
    /// Sizes chunks within 64 KiB and the transport limit, reserving space for the request ID, maximum offset, and protobuf envelope.
    /// </summary>
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

    /// <summary>
    /// Builds a bounded ZIP from worker-local inputs, filtering bootstrap assets and skipping nested links.
    /// The caller owns the rewound, delete-on-close stream; preparation failures close it here.
    /// </summary>
    internal static Task<FileStream> CreateArchiveAsync(CancellationToken cancellationToken)
        => CreateArchiveAsync(cancellationToken, static path => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            MaximumChunkBytes, FileOptions.Asynchronous | FileOptions.SequentialScan));

    /// <summary>
    /// Uses supplied extension input streams while retaining archive validation, resource limits, and stream cleanup ownership.
    /// </summary>
    internal static async Task<FileStream> CreateArchiveAsync(CancellationToken cancellationToken, Func<string, Stream> openExtensionFile)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (openExtensionFile is null)
        {
            throw new ArgumentNullException(nameof(openExtensionFile));
        }

        string root = GetValidatedApplicationDirectory();
        string manifest = Path.Combine(root, "extensions.json");
        string extensions = Path.Combine(root, ".azurefunctions");

        FileStream output = new(Path.Combine(Path.GetTempPath(), TemporaryFilePrefix + Guid.NewGuid().ToString("N") + ".zip"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, MaximumChunkBytes,
            FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
        try
        {
            ZipArchive archive = new(new ArchiveStream(output), ZipArchiveMode.Create, leaveOpen: true);
            Exception? failure = null;
            try
            {
                byte[] buffer = new byte[MaximumChunkBytes];
                int count = 0;
                long contents = await CopyManifestAsync(manifest, AddEntry(archive, "extensions.json", ref count), cancellationToken).ConfigureAwait(false);
                Stack<string> directories = new();
                AddEntry(archive, GetEntryName(root, extensions) + "/", ref count);
                directories.Push(extensions);
                while (directories.Count != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string directory = directories.Pop();
                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    {
                        if (string.Equals(directory, extensions, StringComparison.Ordinal))
                        {
                            throw new InvalidDataException("The extension payload root cannot be a symbolic link or reparse point.");
                        }

                        continue;
                    }

                    foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (string.Equals(path, output.Name, Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        {
                            continue;
                        }

                        FileAttributes attributes = File.GetAttributes(path);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }

                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            AddEntry(archive, GetEntryName(root, path) + "/", ref count);
                            directories.Push(path);
                        }
                        else
                        {
                            string entryName = GetEntryName(root, path);
                            if (IsMetadataLoaderAsset(entryName))
                            {
                                continue;
                            }

                            contents += await CopyFileAsync(path, AddEntry(archive, entryName, ref count), buffer,
                                MaximumContentBytes - contents, cancellationToken, openExtensionFile).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            failure = Dispose(archive, failure);
            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            cancellationToken.ThrowIfCancellationRequested();
            output.Position = 0;
            return output;
        }
        catch (Exception exception)
        {
            ExceptionDispatchInfo.Capture(Dispose(output, exception)!).Throw();
            throw;
        }
    }

    /// <summary>
    /// Reads a bounded manifest, filters its in-memory copy, and writes the result to the archive without changing the source.
    /// Returns the archived byte count for the content budget.
    /// </summary>
    private static async Task<long> CopyManifestAsync(string path, ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        FileStream? input = null;
        Stream? destination = null;
        Exception? failure = null;
        int length = 0;
        try
        {
            input = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, MaximumChunkBytes, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (input.Length > MaximumManifestBytes)
            {
                throw new InvalidDataException("The extension manifest exceeds the 1 MiB input limit.");
            }

            byte[] bytes = new byte[MaximumManifestBytes + 1];
            while (length < bytes.Length)
            {
                int read = await input.ReadAsync(bytes, length, bytes.Length - length, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                length += read;
            }

            if (length > MaximumManifestBytes)
            {
                throw new InvalidDataException("The extension manifest exceeds the 1 MiB input limit.");
            }

            (bytes, length) = FilterManifest(bytes, length, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            destination = entry.Open();
            await destination.WriteAsync(bytes, 0, length, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        failure = Dispose(destination, failure);
        failure = Dispose(input, failure);
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return length;
    }

    /// <summary>
    /// Validates the manifest and removes only metadata-loader startup entries using bounded JSON output.
    /// Preserves other JSON values and returns the original bytes when no filtering is needed.
    /// </summary>
    private static (byte[] Bytes, int Length) FilterManifest(byte[] bytes, int length, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int jsonOffset = length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
        using JsonDocument document = JsonDocument.Parse(new ReadOnlyMemory<byte>(bytes, jsonOffset, length - jsonOffset));
        JsonElement root = document.RootElement;
        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidDataException("The extension manifest must contain an object.");
        }

        int extensionLists = 0;
        bool filterRequired = false;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(property.Name, "extensions", StringComparison.Ordinal))
            {
                continue;
            }

            if (++extensionLists != 1 || property.Value.ValueKind is not JsonValueKind.Array)
            {
                throw new InvalidDataException("The extension manifest must contain one extensions array.");
            }

            foreach (JsonElement extension in property.Value.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (extension.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidDataException("Extension manifest entries must be objects.");
                }

                filterRequired |= IsMetadataLoaderStartup(extension);
            }
        }

        if (extensionLists == 0)
        {
            throw new InvalidDataException("The extension manifest must contain one extensions array.");
        }

        if (!filterRequired)
        {
            return (bytes, length);
        }

        // Rewrite only the archived manifest; fixed storage bounds serialized output as well as input.
        byte[] filtered = new byte[MaximumManifestBytes + 1];
        using MemoryStream output = new(filtered, 0, filtered.Length, writable: true);
        output.SetLength(0);
        Utf8JsonWriter writer = new(output);
        Exception? failure = null;
        try
        {
            writer.WriteStartObject();
            foreach (JsonProperty property in root.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.Equals(property.Name, "extensions", StringComparison.Ordinal))
                {
                    property.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName(property.Name);
                writer.WriteStartArray();
                foreach (JsonElement extension in property.Value.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsMetadataLoaderStartup(extension))
                    {
                        extension.WriteTo(writer);
                    }
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
            writer.Flush();
            if (output.Length > MaximumManifestBytes)
            {
                throw new InvalidDataException("The filtered extension manifest exceeds the 1 MiB output limit.");
            }
        }
        catch (NotSupportedException exception)
        {
            failure = new InvalidDataException("The filtered extension manifest exceeds the 1 MiB output limit.", exception);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        failure = Dispose(writer, failure);
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return (filtered, (int)output.Length);
    }

    /// <summary>
    /// Identifies the SDK bootstrap by its exact startup type and assembly simple name, ignoring version qualifiers.
    /// </summary>
    private static bool IsMetadataLoaderStartup(JsonElement extension)
    {
        if (!extension.TryGetProperty("typeName", out JsonElement typeName) || typeName.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        string qualifiedTypeName = typeName.GetString()!;
        int separator = qualifiedTypeName.IndexOf(',');
        if (separator < 0 || !string.Equals(qualifiedTypeName.Substring(0, separator).Trim(), MetadataLoaderStartupTypeName, StringComparison.Ordinal))
        {
            return false;
        }

        int assemblyEnd = qualifiedTypeName.IndexOf(',', separator + 1);
        int assemblyLength = (assemblyEnd < 0 ? qualifiedTypeName.Length : assemblyEnd) - separator - 1;
        string assemblyName = qualifiedTypeName.Substring(separator + 1, assemblyLength).Trim();
        return string.Equals(assemblyName, MetadataLoaderAssemblyName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Matches only the loader DLL/PDB at the extension root, preserving nested same-named files and shared dependencies.
    /// </summary>
    private static bool IsMetadataLoaderAsset(string entryName)
        => string.Equals(entryName, ".azurefunctions/" + MetadataLoaderAssemblyName + ".dll", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entryName, ".azurefunctions/" + MetadataLoaderAssemblyName + ".pdb", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Copies a file in bounded chunks, checking its initial length and actual reads against the remaining content budget.
    /// Returns the archived byte count and preserves both copy and cleanup failures.
    /// </summary>
    private static async Task<long> CopyFileAsync(string path, ZipArchiveEntry entry, byte[] buffer, long remaining,
        CancellationToken cancellationToken, Func<string, Stream> openExtensionFile)
    {
        Stream? input = null;
        Stream? destination = null;
        Exception? failure = null;
        long copied = 0;
        try
        {
            input = openExtensionFile(path);
            EnsureContentBytesFit(input.Length, remaining);

            destination = entry.Open();
            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                EnsureContentBytesFit(read, remaining - copied);

                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                copied += read;
            }
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        failure = Dispose(destination, failure);
        failure = Dispose(input, failure);
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return copied;
    }

    /// <summary>
    /// Rejects a file length or read count that exceeds the remaining uncompressed content budget.
    /// </summary>
    internal static void EnsureContentBytesFit(long count, long remaining)
    {
        if (count > remaining)
        {
            throw new InvalidDataException("An extension payload file exceeds the available input size limit.");
        }
    }

    /// <summary>
    /// Rejects writes that would exceed the compressed ZIP limit, including writes of ZIP metadata.
    /// </summary>
    internal static void EnsureArchiveBytesFit(long written, int count)
    {
        if (count > MaximumArchiveBytes - written)
        {
            throw new InvalidDataException("The extension payload exceeds the 256 MiB ZIP limit.");
        }
    }

    /// <summary>
    /// Enforces path depth and counts each file or directory against the entry limit before creating its ZIP entry.
    /// </summary>
    internal static ZipArchiveEntry AddEntry(ZipArchive archive, string name, ref int count)
    {
        if (name.TrimEnd('/').Split('/').Length > MaximumPathDepth)
        {
            throw new InvalidDataException("The extension payload exceeds the 32-segment path depth limit.");
        }

        if (++count > MaximumEntries)
        {
            throw new InvalidDataException("The extension payload exceeds the 10000 entry limit.");
        }

        return archive.CreateEntry(name, CompressionLevel.Optimal);
    }

    /// <summary>
    /// Reads root input attributes and rejects symbolic links or reparse points before they can become archive inputs.
    /// </summary>
    private static FileAttributes ValidateEntry(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Extension payload roots cannot be symbolic links or reparse points.");
        }

        return attributes;
    }

    /// <summary>
    /// Resolves the worker-local root, validates input types and links, and probes manifest access and directory enumeration.
    /// </summary>
    private static string GetValidatedApplicationDirectory()
    {
        string root = WorkerApplicationDirectory.GetRequired();
        if ((ValidateEntry(root) & FileAttributes.Directory) == 0)
        {
            throw new InvalidDataException("The worker-local application root must be a directory.");
        }

        string manifest = Path.Combine(root, "extensions.json");
        if ((ValidateEntry(manifest) & FileAttributes.Directory) != 0)
        {
            throw new InvalidDataException("The extension payload manifest must be a regular file.");
        }

        string extensions = Path.Combine(root, ".azurefunctions");
        if ((ValidateEntry(extensions) & FileAttributes.Directory) == 0)
        {
            throw new InvalidDataException("The extension payload requires a .azurefunctions directory.");
        }

        using FileStream input = File.Open(manifest, FileMode.Open, FileAccess.Read, FileShare.Read);
        using IEnumerator<string> entries = Directory.EnumerateFileSystemEntries(extensions).GetEnumerator();
        entries.MoveNext();
        return root;
    }

    /// <summary>
    /// Builds a root-relative, forward-slash ZIP name and rejects backslashes or drive syntax that would make it non-portable.
    /// </summary>
    private static string GetEntryName(string root, string path)
    {
        string name = path.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/');
        if (name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0)
        {
            throw new InvalidDataException("An extension payload entry has a non-portable path.");
        }

        return name;
    }

    /// <summary>
    /// Disposes one resource while preserving an earlier failure, aggregating both exceptions if cleanup also fails.
    /// </summary>
    private static Exception? Dispose(IDisposable? resource, Exception? failure)
    {
        try
        {
            resource?.Dispose();
        }
        catch (Exception exception)
        {
            return failure is null ? exception : new AggregateException(failure, exception);
        }

        return failure;
    }

    /// <summary>
    /// Owns one transfer's linked shutdown cancellation and shared completion task.
    /// </summary>
    private sealed class Transfer(string requestId, CancellationToken connectionClosed, CancellationToken stopping)
    {
        public string RequestId { get; } = requestId;
        public CancellationTokenSource Cancellation { get; } = CancellationTokenSource.CreateLinkedTokenSource(connectionClosed, stopping);
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Provides non-seekable ZIP output with a size check on every write, leaving ownership of the underlying file to the caller.
    /// </summary>
    private sealed class ArchiveStream(FileStream file) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => file.Length;
        public override long Position { get => file.Position; set => throw new NotSupportedException(); }
        public override void Flush() => file.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => file.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureArchiveBytesFit(file.Position, count);
            file.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            EnsureArchiveBytesFit(file.Position, count);
            return file.WriteAsync(buffer, offset, count, cancellationToken);
        }
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
