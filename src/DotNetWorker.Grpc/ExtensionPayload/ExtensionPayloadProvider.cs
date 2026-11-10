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
using static Microsoft.Azure.Functions.Worker.Grpc.ExtensionPayloadCleanup;
using static Microsoft.Azure.Functions.Worker.Grpc.ExtensionPayloadLimits;

namespace Microsoft.Azure.Functions.Worker.Grpc;

/// <summary>
/// Opens filtered extension archives on demand, with temporary resources owned by the returned stream.
/// </summary>
internal sealed class ExtensionPayloadProvider : IExtensionPayloadProvider
{
    private const int CopyBufferSize = 64 * 1024;
    internal const long MaximumContentBytes = 1024L * 1024 * 1024;
    internal const int MaximumManifestBytes = 1024 * 1024;
    internal const int MaximumEntries = 10000;
    internal const int MaximumPathDepth = 32;
    internal const string TemporaryDirectoryPrefix = "functions-extension-payload-";

    private const string MetadataLoaderAssemblyName = "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader";
    private const string MetadataLoaderStartupTypeName = MetadataLoaderAssemblyName + ".Startup";

    /// <summary>
    /// Builds a bounded ZIP from worker-local inputs, filtering bootstrap assets and skipping nested links.
    /// The caller owns the rewound stream and its private temporary directory; preparation failures clean them up here.
    /// </summary>
    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        => OpenReadAsync(static path => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan), cancellationToken);

    /// <summary>
    /// Uses supplied extension input streams while retaining archive validation, resource limits, and stream cleanup ownership.
    /// </summary>
    internal async Task<Stream> OpenReadAsync(Func<string, Stream> openExtensionFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (openExtensionFile is null)
        {
            throw new ArgumentNullException(nameof(openExtensionFile));
        }

        string root = GetValidatedApplicationDirectory();
        string manifest = Path.Combine(root, "extensions.json");
        string extensions = Path.Combine(root, ".azurefunctions");

        FileStream output = CreateTemporaryArchive();
        string temporaryDirectory = Path.GetDirectoryName(output.Name)!;
        try
        {
            ZipArchive archive = new(new ArchiveStream(output), ZipArchiveMode.Create, leaveOpen: true);
            Exception? failure = null;
            try
            {
                byte[] buffer = new byte[CopyBufferSize];
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
                        if (string.Equals(path, temporaryDirectory,
                            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
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
                                MaximumContentBytes - contents, openExtensionFile, cancellationToken).ConfigureAwait(false);
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
    /// Creates a ZIP in a private temporary directory whose lifetime is owned by the returned stream.
    /// </summary>
    private static FileStream CreateTemporaryArchive()
    {
#if NET7_0_OR_GREATER
        DirectoryInfo directory = Directory.CreateTempSubdirectory(TemporaryDirectoryPrefix);
        try
        {
            return new TemporaryArchiveStream(directory);
        }
        catch (Exception exception)
        {
            ExceptionDispatchInfo.Capture(DeleteDirectory(directory, exception)!).Throw();
            throw;
        }
#else
        throw new NotSupportedException("Extension payload transfer requires .NET 7 or later.");
#endif
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
            input = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            (byte[] bytes, int bytesRead) = await ReadManifestAsync(input, cancellationToken).ConfigureAwait(false);
            (bytes, length) = FilterManifest(bytes, bytesRead, cancellationToken);
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

    internal static async Task<(byte[] Bytes, int Length)> ReadManifestAsync(Stream input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long initialLength = input.Length;
        if (initialLength > MaximumManifestBytes)
        {
            throw new InvalidDataException("The extension manifest exceeds the 1 MiB input limit.");
        }

        // The extra byte detects growth beyond the initial length, including at the size limit.
        byte[] bytes = new byte[checked((int)initialLength + 1)];
        int length = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (length == bytes.Length)
            {
                Array.Resize(ref bytes, Math.Min(bytes.Length * 2, MaximumManifestBytes + 1));
            }

            int read = await input.ReadAsync(bytes, length, bytes.Length - length, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            length += read;
            if (length > MaximumManifestBytes)
            {
                throw new InvalidDataException("The extension manifest exceeds the 1 MiB input limit.");
            }
        }

        return (bytes, length);
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
        Func<string, Stream> openExtensionFile, CancellationToken cancellationToken)
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
        string root = AppContext.BaseDirectory;
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
        int start = root.Length;
        if (start < path.Length
            && (path[start] == Path.DirectorySeparatorChar || path[start] == Path.AltDirectorySeparatorChar))
        {
            start++;
        }

        string name = path.Substring(start).Replace(Path.DirectorySeparatorChar, '/');
        if (name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0)
        {
            throw new InvalidDataException("An extension payload entry has a non-portable path.");
        }

        return name;
    }

    /// <summary>
    /// Deletes an owned temporary directory while preserving any earlier failure.
    /// </summary>
    private static Exception? DeleteDirectory(DirectoryInfo? directory, Exception? failure)
    {
        try
        {
            directory?.Delete();
        }
        catch (Exception exception)
        {
            return failure is null ? exception : new AggregateException(failure, exception);
        }

        return failure;
    }

    /// <summary>
    /// Deletes the ZIP on close and removes its private directory after the underlying file has been disposed.
    /// </summary>
    private sealed class TemporaryArchiveStream(DirectoryInfo directory)
        : FileStream(Path.Combine(directory.FullName, "extensions.zip"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose)
    {
        private DirectoryInfo? _directory = directory;

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                base.Dispose(false);
                return;
            }

            Exception? failure = null;
            try
            {
                base.Dispose(true);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            failure = DeleteDirectory(Interlocked.Exchange(ref _directory, null), failure);
            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
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
}
