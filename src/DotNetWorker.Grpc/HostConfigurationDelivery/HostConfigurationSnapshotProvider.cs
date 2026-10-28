// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Text;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.HostConfigurationDelivery
{
    /// <summary>
    /// Process-wide Host configuration snapshot (currently host.json) sent in worker initialization responses
    /// only when the host requests it with the HostConfiguration capability.
    /// </summary>
    /// <remarks>
    /// Captured once on the first requested init and replayed afterwards, including failures (restart to retry).
    /// App content is treated as immutable for the process lifetime. The file is converted to strict BOM-free UTF-8 but never
    /// parsed or logged here;
    /// the host owns interpretation. The ASP.NET Core extension reads the route prefix independently, so changing
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

        public HostConfigurationSnapshotProvider()
            : this(() => Environment.GetEnvironmentVariable(AppDirectoryVariableName), new PhysicalHostConfigurationFileSystem())
        {
        }

        internal HostConfigurationSnapshotProvider(Func<string?> appDirectoryAccessor, IHostConfigurationFileSystem fileSystem)
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
                () => Capture(appDirectoryAccessor, fileSystem),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Returns a new envelope only when the host requested it with HostConfiguration = "1";
        /// otherwise returns null without reading host.json or initializing the snapshot.
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
                return HostConfigurationSnapshot.Error;
            }

            return Read(appDirectory, fileSystem);
        }

        internal static HostConfigurationSnapshot Read(string? appDirectory, IHostConfigurationFileSystem fileSystem)
        {
            string path;

            try
            {
                // Relative roots resolve with native platform semantics; an unset root is never replaced.
                if (appDirectory is null || string.IsNullOrWhiteSpace(appDirectory))
                {
                    return HostConfigurationSnapshot.Error;
                }

                path = Path.Combine(appDirectory, HostJsonFileName);
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.Error;
            }

            try
            {
                using Stream stream = fileSystem.OpenRead(path);
                return ReadBounded(stream);
            }
            catch (FileNotFoundException)
            {
                return HostConfigurationSnapshot.Missing;
            }
            catch (Exception)
            {
                // Content-free on purpose: exception messages can carry file paths.
                return HostConfigurationSnapshot.Error;
            }
        }

        /// <summary>
        /// Converts the source file (UTF-8 default, or a UTF-8/UTF-16/UTF-32 BOM) incrementally to strict BOM-free UTF-8,
        /// without trusting the stream length (the file can grow while read and not every stream is seekable).
        /// Memory is bounded by the converted payload cap: reading stops once the output exceeds
        /// <see cref="MaxHostConfigurationBytes"/>. Invalid encoding throws, which the caller maps to an Error status.
        /// The JSON itself is not parsed.
        /// </summary>
        internal static HostConfigurationSnapshot ReadBounded(Stream stream)
        {
            byte[] buffer = new byte[4096];

            int head = 0;
            while (head < 4)
            {
                int n = stream.Read(buffer, head, 4 - head);
                if (n <= 0)
                {
                    break;
                }

                head += n;
            }

            Encoding sourceEncoding = DetectEncoding(buffer, head, out int bomLength);
            Decoder decoder = sourceEncoding.GetDecoder();
            Encoder encoder = StrictUtf8.GetEncoder();
            char[] chars = new char[sourceEncoding.GetMaxCharCount(buffer.Length)];
            byte[] encoded = new byte[StrictUtf8.GetMaxByteCount(chars.Length + 1)];
            using var content = new MemoryStream();

            int offset = bomLength;
            int count = head - bomLength;

            while (true)
            {
                int charCount = decoder.GetChars(buffer, offset, count, chars, 0, flush: false);
                int byteCount = encoder.GetBytes(chars, 0, charCount, encoded, 0, flush: false);
                content.Write(encoded, 0, byteCount);

                if (content.Length > MaxHostConfigurationBytes)
                {
                    return HostConfigurationSnapshot.TooLarge;
                }

                offset = 0;
                count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0)
                {
                    break;
                }
            }

            // Flushing throws on a truncated trailing sequence (strict decoders).
            int tailChars = decoder.GetChars(buffer, 0, 0, chars, 0, flush: true);
            int tailBytes = encoder.GetBytes(chars, 0, tailChars, encoded, 0, flush: true);
            content.Write(encoded, 0, tailBytes);

            if (content.Length > MaxHostConfigurationBytes)
            {
                return HostConfigurationSnapshot.TooLarge;
            }

            return HostConfigurationSnapshot.Available(ByteString.CopyFrom(content.GetBuffer(), 0, (int)content.Length));
        }

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private static Encoding DetectEncoding(byte[] b, int count, out int bomLength)
        {
            // UTF-32LE must be tested before UTF-16LE: both start with FF FE.
            if (count >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0 && b[3] == 0)
            {
                bomLength = 4;
                return new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);
            }

            if (count >= 4 && b[0] == 0 && b[1] == 0 && b[2] == 0xFE && b[3] == 0xFF)
            {
                bomLength = 4;
                return new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);
            }

            if (count >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
            {
                bomLength = 3;
                return StrictUtf8;
            }

            if (count >= 2 && b[0] == 0xFF && b[1] == 0xFE)
            {
                bomLength = 2;
                return new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
            }

            if (count >= 2 && b[0] == 0xFE && b[1] == 0xFF)
            {
                bomLength = 2;
                return new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);
            }

            bomLength = 0;
            return StrictUtf8;
        }
    }

    internal sealed class HostConfigurationSnapshot
    {
        private HostConfigurationSnapshot(HostConfiguration.Types.Status status, ByteString content)
        {
            Status = status;
            Content = content;
        }

        public HostConfiguration.Types.Status Status { get; }

        public ByteString Content { get; }

        public static HostConfigurationSnapshot Missing { get; } = new(HostConfiguration.Types.Status.Missing, ByteString.Empty);

        public static HostConfigurationSnapshot Error { get; } = new(HostConfiguration.Types.Status.Error, ByteString.Empty);

        public static HostConfigurationSnapshot TooLarge { get; } = new(HostConfiguration.Types.Status.TooLarge, ByteString.Empty);

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
