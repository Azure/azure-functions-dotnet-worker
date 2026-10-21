// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.HostJson
{
    /// <summary>
    /// Provides the process-wide host.json snapshot sent to the host when it requests it during worker initialization.
    /// </summary>
    /// <remarks>
    /// The file is read at most once per process, on the first negotiated worker init request, and the outcome
    /// (including failures) is replayed for every later init request. A cached failure is only retried by restarting
    /// the worker process. The worker treats the deployed app content as immutable for the lifetime of the process.
    /// The document is never parsed, decoded, or logged here; the host owns interpretation.
    /// The ASP.NET Core integration extension reads host.json's route prefix independently and lazily; it does not
    /// consume this snapshot, so mutating host.json while the process runs is unsupported.
    /// </remarks>
    internal sealed class HostJsonSnapshotProvider
    {
        internal const string CapabilityName = "HostJson";
        internal const string SupportedCapabilityValue = "1";
        internal const string AppDirectoryVariableName = "FUNCTIONS_APPLICATION_DIRECTORY";
        internal const string HostJsonFileName = "host.json";

        // Includes any byte order mark. The cap applies to raw bytes actually read.
        internal const int MaxHostJsonBytes = 64 * 1024;

        private readonly Lazy<HostJsonSnapshot> _snapshot;

        public HostJsonSnapshotProvider()
            : this(() => Environment.GetEnvironmentVariable(AppDirectoryVariableName), new PhysicalHostJsonFileSystem())
        {
        }

        internal HostJsonSnapshotProvider(Func<string?> appDirectoryAccessor, IHostJsonFileSystem fileSystem)
        {
            if (appDirectoryAccessor is null)
            {
                throw new ArgumentNullException(nameof(appDirectoryAccessor));
            }

            if (fileSystem is null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _snapshot = new Lazy<HostJsonSnapshot>(
                () => Read(appDirectoryAccessor(), fileSystem),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Returns the host.json envelope only when the host advertised a supported "HostJson" capability.
        /// Otherwise returns null and performs no file access.
        /// </summary>
        public HostJsonConfiguration? TryGetConfiguration(WorkerInitRequest request)
        {
            if (!request.Capabilities.TryGetValue(CapabilityName, out string? value)
                || !string.Equals(value, SupportedCapabilityValue, StringComparison.Ordinal))
            {
                return null;
            }

            HostJsonSnapshot snapshot = _snapshot.Value;

            var configuration = new HostJsonConfiguration { Status = snapshot.Status };
            if (snapshot.Status == HostJsonConfiguration.Types.Status.Available)
            {
                configuration.Content = snapshot.Content;
            }

            return configuration;
        }

        internal static HostJsonSnapshot Read(string? appDirectory, IHostJsonFileSystem fileSystem)
        {
            string path;

            try
            {
                if (appDirectory is null
                    || string.IsNullOrWhiteSpace(appDirectory)
                    || !Path.IsPathRooted(appDirectory)
                    || !fileSystem.DirectoryExists(appDirectory))
                {
                    return HostJsonSnapshot.AppDirectoryUnavailable;
                }

                path = Path.Combine(appDirectory, HostJsonFileName);
            }
            catch (Exception)
            {
                return HostJsonSnapshot.AppDirectoryUnavailable;
            }

            try
            {
                using Stream stream = fileSystem.OpenRead(path);
                return ReadBounded(stream, MaxHostJsonBytes);
            }
            catch (FileNotFoundException)
            {
                return HostJsonSnapshot.Missing;
            }
            catch (DirectoryNotFoundException)
            {
                // The app directory existed a moment ago but is gone now.
                return HostJsonSnapshot.AppDirectoryUnavailable;
            }
            catch (Exception)
            {
                // Intentionally content-free: exception messages can carry file paths.
                return HostJsonSnapshot.ReadError;
            }
        }

        /// <summary>
        /// Reads at most <paramref name="limit"/> bytes and fails if more data exists. The stream length is not trusted
        /// because the file can grow while it is being read, and not every stream is seekable.
        /// </summary>
        internal static HostJsonSnapshot ReadBounded(Stream stream, int limit)
        {
            // Never buffer beyond the limit plus a single probe byte.
            byte[] buffer = new byte[Math.Min(limit + 1, 4096)];
            using var content = new MemoryStream();

            while (true)
            {
                int remaining = limit + 1 - (int)content.Length;
                int read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));

                if (read <= 0)
                {
                    break;
                }

                content.Write(buffer, 0, read);

                if (content.Length > limit)
                {
                    return HostJsonSnapshot.TooLarge;
                }
            }

            return HostJsonSnapshot.Available(ByteString.CopyFrom(content.GetBuffer(), 0, (int)content.Length));
        }
    }

    internal sealed class HostJsonSnapshot
    {
        private HostJsonSnapshot(HostJsonConfiguration.Types.Status status, ByteString content)
        {
            Status = status;
            Content = content;
        }

        public HostJsonConfiguration.Types.Status Status { get; }

        public ByteString Content { get; }

        public static HostJsonSnapshot Missing { get; } = new(HostJsonConfiguration.Types.Status.Missing, ByteString.Empty);

        public static HostJsonSnapshot AppDirectoryUnavailable { get; } = new(HostJsonConfiguration.Types.Status.AppDirectoryUnavailable, ByteString.Empty);

        public static HostJsonSnapshot ReadError { get; } = new(HostJsonConfiguration.Types.Status.ReadError, ByteString.Empty);

        public static HostJsonSnapshot TooLarge { get; } = new(HostJsonConfiguration.Types.Status.TooLarge, ByteString.Empty);

        public static HostJsonSnapshot Available(ByteString content) => new(HostJsonConfiguration.Types.Status.Available, content);
    }

    internal interface IHostJsonFileSystem
    {
        bool DirectoryExists(string path);

        Stream OpenRead(string path);
    }

    internal sealed class PhysicalHostJsonFileSystem : IHostJsonFileSystem
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public Stream OpenRead(string path)
            => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, FileOptions.SequentialScan);
    }
}
