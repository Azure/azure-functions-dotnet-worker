// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
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
    /// App content is treated as immutable for the process lifetime. The document is never parsed, decoded or logged here;
    /// the host owns interpretation. The ASP.NET Core extension reads the route prefix independently, so changing
    /// host.json while the process runs is unsupported.
    /// </remarks>
    internal sealed class HostConfigurationSnapshotProvider
    {
        internal const string AppDirectoryVariableName = "FUNCTIONS_APPLICATION_DIRECTORY";
        internal const string CapabilityName = "HostConfiguration";
        internal const string SupportedCapabilityValue = "1";
        internal const string HostJsonFileName = "host.json";

        // Raw bytes, including any BOM.
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

        // An accessor failure is cached as a content-free ReadError instead of faulting the Lazy.
        private static HostConfigurationSnapshot Capture(Func<string?> appDirectoryAccessor, IHostConfigurationFileSystem fileSystem)
        {
            string? appDirectory;

            try
            {
                appDirectory = appDirectoryAccessor();
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.ReadError;
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
                    return HostConfigurationSnapshot.ReadError;
                }

                path = Path.Combine(appDirectory, HostJsonFileName);
            }
            catch (Exception)
            {
                return HostConfigurationSnapshot.ReadError;
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
                return HostConfigurationSnapshot.ReadError;
            }
        }

        /// <summary>
        /// Reads at most <see cref="MaxHostConfigurationBytes"/> bytes (plus one probe byte) without trusting the stream length,
        /// since the file can grow while read and not every stream is seekable.
        /// </summary>
        internal static HostConfigurationSnapshot ReadBounded(Stream stream)
        {
            byte[] buffer = new byte[4096];
            using var content = new MemoryStream();

            while (true)
            {
                int remaining = MaxHostConfigurationBytes + 1 - (int)content.Length;
                int read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));

                if (read <= 0)
                {
                    break;
                }

                content.Write(buffer, 0, read);

                if (content.Length > MaxHostConfigurationBytes)
                {
                    return HostConfigurationSnapshot.TooLarge;
                }
            }

            return HostConfigurationSnapshot.Available(ByteString.CopyFrom(content.GetBuffer(), 0, (int)content.Length));
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

        public static HostConfigurationSnapshot ReadError { get; } = new(HostConfiguration.Types.Status.ReadError, ByteString.Empty);

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
