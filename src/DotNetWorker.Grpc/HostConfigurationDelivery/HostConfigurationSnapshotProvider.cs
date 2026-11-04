// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Grpc.HostConfigurationDelivery
{
    /// <summary>
    /// Process-wide Host configuration snapshot (currently host.json) sent in worker initialization responses
    /// only when the host requests it with the HostConfiguration capability.
    /// </summary>
    /// <remarks>
    /// Captured once on the first requested init and replayed afterwards, including failures (restart to retry).
    /// App content is treated as immutable for the process lifetime. The file is converted to strict BOM-free UTF-8 and
    /// normalized to strict JSON (comments and trailing commas removed); the host owns interpretation. A failure is
    /// logged once with a fixed reason code only, never with paths, exception messages or file content.
    /// The ASP.NET Core extension reads the route prefix independently, so changing
    /// host.json while the process runs is unsupported.
    /// </remarks>
    internal sealed class HostConfigurationSnapshotProvider
    {
        internal const string AppDirectoryVariableName = "FUNCTIONS_APPLICATION_DIRECTORY";
        internal const string CapabilityName = "HostConfiguration";
        internal const string SupportedCapabilityValue = "1";
        internal const string HostJsonFileName = "host.json";

        // Maximum transmitted payload: converted BOM-free UTF-8 bytes.
        internal const int MaxHostConfigurationBytes = 64 * 1024;

        private readonly Lazy<HostConfigurationSnapshot> _snapshot;

        public HostConfigurationSnapshotProvider(ILogger<HostConfigurationSnapshotProvider> logger)
            : this(() => Environment.GetEnvironmentVariable(AppDirectoryVariableName), new PhysicalHostConfigurationFileSystem(), logger)
        {
        }

        internal HostConfigurationSnapshotProvider(
            Func<string?> appDirectoryAccessor,
            IHostConfigurationFileSystem fileSystem,
            ILogger? logger = null)
        {
            if (appDirectoryAccessor is null)
            {
                throw new ArgumentNullException(nameof(appDirectoryAccessor));
            }

            if (fileSystem is null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _snapshot = new Lazy<HostConfigurationSnapshot>(
                () => LogFailure(Capture(appDirectoryAccessor, fileSystem), logger),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Returns a new envelope only when the host requested it with HostConfiguration = "1";
        /// otherwise returns null and neither reads host.json nor initializes the snapshot.
        /// </summary>
        public HostConfiguration? TryGetConfiguration(WorkerInitRequest request)
        {
            if (!request.Capabilities.TryGetValue(CapabilityName, out string? value)
                || !string.Equals(value, SupportedCapabilityValue, StringComparison.Ordinal))
            {
                return null;
            }

            HostConfigurationSnapshot snapshot = _snapshot.Value;

            var configuration = new HostConfiguration { Status = snapshot.Status };
            if (snapshot.Status == HostConfiguration.Types.Status.Available)
            {
                configuration.Content = snapshot.Content;
            }

            return configuration;
        }

        // An accessor failure is cached as a content-free Error instead of faulting the Lazy.
        private static HostConfigurationSnapshot Capture(Func<string?> appDirectoryAccessor, IHostConfigurationFileSystem fileSystem)
        {
            string? appDirectory;

            try
            {
                appDirectory = appDirectoryAccessor();
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.AppDirectoryUnavailable);
            }

            return Read(appDirectory, fileSystem);
        }

        // Fixed reason codes only: paths, exception messages and file content never reach the logs.
        private static HostConfigurationSnapshot LogFailure(HostConfigurationSnapshot snapshot, ILogger? logger)
        {
            if (logger is not null && snapshot.Reason != HostConfigurationFailureReason.None)
            {
                logger.LogWarning(
                    "Host configuration could not be delivered to the host. Reason: {Reason}. The failure is retained until the worker process restarts.",
                    snapshot.Reason.ToString());
            }

            return snapshot;
        }

        internal static HostConfigurationSnapshot Read(string? appDirectory, IHostConfigurationFileSystem fileSystem)
        {
            string path;

            try
            {
                // Relative roots resolve with native platform semantics; an unset root is never replaced.
                if (appDirectory is null || string.IsNullOrWhiteSpace(appDirectory))
                {
                    return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.AppDirectoryNotSet);
                }

                path = Path.Combine(appDirectory, HostJsonFileName);
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.AppDirectoryInvalid);
            }

            Stream stream;

            try
            {
                stream = fileSystem.OpenRead(path);
            }
            catch (FileNotFoundException)
            {
                return HostConfigurationSnapshot.Missing;
            }
            catch (DirectoryNotFoundException)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.AppDirectoryNotFound);
            }
            catch (UnauthorizedAccessException)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.AccessDenied);
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.OpenFailed);
            }

            try
            {
                using (stream)
                {
                    return ReadBounded(stream);
                }
            }
            catch (DecoderFallbackException)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.InvalidEncoding);
            }
            catch (InvalidEncodingException)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.InvalidEncoding);
            }
            catch (JsonException)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.InvalidJson);
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.ReadFailed);
            }
        }

        private sealed class InvalidEncodingException : Exception
        {
        }

        /// <summary>
        /// Converts the source file (UTF-8 default, or a UTF-8/UTF-16/UTF-32 BOM) incrementally to strict BOM-free UTF-8,
        /// without trusting the stream length (the file can grow while read and not every stream is seekable).
        /// Memory is bounded by the converted payload cap: reading stops once the output exceeds
        /// <see cref="MaxHostConfigurationBytes"/>. Invalid encoding throws, which the caller maps to an Error status.
        /// Non-empty content is then normalized to a strict JSON object (comments and trailing commas removed);
        /// empty content and non-object roots are errors because the host rejects them.
        /// </summary>
        /// <remarks>
        /// <see cref="StreamReader"/> detects the BOM; the BOM-less default is a throwing UTF-8 decoder. Decoders that
        /// <see cref="StreamReader"/> selects from a BOM replace invalid input with U+FFFD, so U+FFFD in text from a
        /// BOM-detected source is rejected as invalid encoding.
        /// </remarks>
        internal static HostConfigurationSnapshot ReadBounded(Stream stream)
        {
            using var reader = new StreamReader(new HeadFilledStream(stream), StrictUtf8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            Encoder encoder = StrictUtf8.GetEncoder();
            char[] chars = new char[4096];
            byte[] encoded = new byte[StrictUtf8.GetMaxByteCount(chars.Length + 1)];
            using var content = new MemoryStream();

            while (true)
            {
                int charCount = reader.Read(chars, 0, chars.Length);
                bool last = charCount <= 0;
                if (last)
                {
                    charCount = 0;
                }

                // The detected encoding is only known after the first read.
                if (!ReferenceEquals(reader.CurrentEncoding, StrictUtf8) && Array.IndexOf(chars, '\uFFFD', 0, charCount) >= 0)
                {
                    throw new InvalidEncodingException();
                }

                int byteCount = encoder.GetBytes(chars, 0, charCount, encoded, 0, flush: last);
                content.Write(encoded, 0, byteCount);

                if (content.Length > MaxHostConfigurationBytes)
                {
                    return HostConfigurationSnapshot.TooLarge;
                }

                if (last)
                {
                    break;
                }
            }

            // Empty or BOM-only content is not a JSON object; the host rejects it, so report it as an error.
            if (content.Length == 0)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.InvalidJson);
            }

            return Normalize(content.GetBuffer(), (int)content.Length);
        }

        /// <summary>
        /// The source may use comments and trailing commas, which strict parsers reject. Re-emit it as strict compact JSON
        /// so delivery does not narrow the accepted syntax. String and number tokens are copied byte-for-byte (no
        /// re-escaping). Syntax errors throw (mapped to Error by the caller); the content is never logged.
        /// The cap applies again to the normalized payload.
        /// </summary>
        private static HostConfigurationSnapshot Normalize(byte[] utf8, int length)
        {
            var reader = new Utf8JsonReader(
                new ReadOnlySpan<byte>(utf8, 0, length),
                new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            using var output = new MemoryStream(length);
            bool needComma = false;
            bool first = true;

            while (reader.Read())
            {
                // The host requires a JSON object root.
                if (first && reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                first = false;
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        WriteSeparator(output, ref needComma);
                        output.WriteByte(reader.TokenType == JsonTokenType.StartObject ? (byte)'{' : (byte)'[');
                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        output.WriteByte(reader.TokenType == JsonTokenType.EndObject ? (byte)'}' : (byte)']');
                        needComma = true;
                        break;
                    case JsonTokenType.PropertyName:
                        WriteSeparator(output, ref needComma);
                        WriteQuoted(output, reader.ValueSpan);
                        output.WriteByte((byte)':');
                        break;
                    case JsonTokenType.String:
                        WriteSeparator(output, ref needComma);
                        WriteQuoted(output, reader.ValueSpan);
                        needComma = true;
                        break;
                    default:
                        WriteSeparator(output, ref needComma);
                        WriteSpan(output, reader.ValueSpan);
                        needComma = true;
                        break;
                }
            }

            if (output.Length > MaxHostConfigurationBytes)
            {
                return HostConfigurationSnapshot.TooLarge;
            }

            return HostConfigurationSnapshot.Available(ByteString.CopyFrom(output.GetBuffer(), 0, (int)output.Length));
        }

        private static void WriteSeparator(MemoryStream output, ref bool needComma)
        {
            if (needComma)
            {
                output.WriteByte((byte)',');
            }

            needComma = false;
        }

        private static void WriteQuoted(MemoryStream output, ReadOnlySpan<byte> value)
        {
            output.WriteByte((byte)'"');
            WriteSpan(output, value);
            output.WriteByte((byte)'"');
        }

        private static void WriteSpan(MemoryStream output, ReadOnlySpan<byte> value)
        {
            byte[] bytes = value.ToArray();
            output.Write(bytes, 0, bytes.Length);
        }

        // StreamReader only detects a BOM when its first read returns at least 2 bytes (4 for UTF-32). A stream may
        // legally return fewer, so the head is gathered here and served as a single first read.
        private sealed class HeadFilledStream : Stream
        {
            private readonly Stream _inner;
            private readonly byte[] _head = new byte[4];
            private int _headLength;
            private int _headOffset;

            public HeadFilledStream(Stream inner)
            {
                _inner = inner;
                while (_headLength < _head.Length)
                {
                    int n = inner.Read(_head, _headLength, _head.Length - _headLength);
                    if (n <= 0)
                    {
                        break;
                    }

                    _headLength += n;
                }
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_headOffset < _headLength)
                {
                    int n = Math.Min(count, _headLength - _headOffset);
                    Buffer.BlockCopy(_head, _headOffset, buffer, offset, n);
                    _headOffset += n;
                    return n;
                }

                return _inner.Read(buffer, offset, count);
            }
        }

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    }

    // Content-free diagnostic reasons logged when a failure is cached; never carry paths or content.
    internal enum HostConfigurationFailureReason
    {
        None = 0,
        AppDirectoryUnavailable,
        AppDirectoryNotSet,
        AppDirectoryInvalid,
        AppDirectoryNotFound,
        AccessDenied,
        OpenFailed,
        ReadFailed,
        InvalidEncoding,
        InvalidJson,
        Oversized,
    }

    internal sealed class HostConfigurationSnapshot
    {
        private HostConfigurationSnapshot(HostConfiguration.Types.Status status, ByteString content, HostConfigurationFailureReason reason = HostConfigurationFailureReason.None)
        {
            Status = status;
            Content = content;
            Reason = reason;
        }

        public HostConfiguration.Types.Status Status { get; }

        public ByteString Content { get; }

        public HostConfigurationFailureReason Reason { get; }

        public static HostConfigurationSnapshot Missing { get; } = new(HostConfiguration.Types.Status.Missing, ByteString.Empty);

        public static HostConfigurationSnapshot TooLarge { get; } = new(HostConfiguration.Types.Status.TooLarge, ByteString.Empty, HostConfigurationFailureReason.Oversized);

        public static HostConfigurationSnapshot Failure(HostConfigurationFailureReason reason) => new(HostConfiguration.Types.Status.Error, ByteString.Empty, reason);

        public static HostConfigurationSnapshot Available(ByteString content) => new(HostConfiguration.Types.Status.Available, content);
    }

    internal interface IHostConfigurationFileSystem
    {
        Stream OpenRead(string path);
    }

    internal sealed class PhysicalHostConfigurationFileSystem : IHostConfigurationFileSystem
    {
        public Stream OpenRead(string path)
            => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, FileOptions.SequentialScan);
    }
}
