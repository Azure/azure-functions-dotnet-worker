// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    /// <summary>
    /// Captures and caches the application's host configuration independently of the transport.
    /// </summary>
    /// <remarks>
    /// Captured once on first access and replayed afterwards, including failures (restart to retry).
    /// App content is treated as immutable for the process lifetime. The UTF-8 file in the application directory is
    /// normalized to strict JSON (comments and trailing commas removed); the host owns interpretation. A failure is
    /// logged once with a fixed reason code only, never with paths, exception messages or file content.
    /// The ASP.NET Core extension reads the route prefix independently, so changing
    /// host.json while the process runs is unsupported.
    /// </remarks>
    internal sealed class HostConfigurationSnapshotProvider
    {
        internal const string HostJsonFileName = "host.json";

        private readonly Lazy<HostConfigurationSnapshot> _snapshot;

        public HostConfigurationSnapshotProvider(ILogger<HostConfigurationSnapshotProvider> logger)
            : this(new PhysicalHostConfigurationFileSystem(), logger)
        {
        }

        internal HostConfigurationSnapshotProvider(IHostConfigurationFileSystem fileSystem, ILogger? logger = null)
        {
            if (fileSystem is null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _snapshot = new Lazy<HostConfigurationSnapshot>(
                () => LogFailure(Read(fileSystem), logger),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Returns the cached snapshot, reading host.json only on first access.
        /// </summary>
        public HostConfigurationSnapshot GetSnapshot() => _snapshot.Value;

        // Fixed reason codes only: paths, exception messages and file content never reach the logs.
        private static HostConfigurationSnapshot LogFailure(HostConfigurationSnapshot snapshot, ILogger? logger)
        {
            if (logger is not null && snapshot.Reason != HostConfigurationFailureReason.None)
            {
                logger.LogWarning(
                    "Host configuration could not be captured. Reason: {Reason}. The failure is retained until the worker process restarts.",
                    snapshot.Reason.ToString());
            }

            return snapshot;
        }

        internal static HostConfigurationSnapshot Read(IHostConfigurationFileSystem fileSystem)
        {
            try
            {
                using Stream stream = fileSystem.OpenRead(Path.Combine(ApplicationDirectory.GetApplicationDirectory(), HostJsonFileName));
                using Stream utf8 = HostConfigurationReader.Read(stream);
                using JsonDocument document = JsonDocument.Parse(utf8,
                    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.InvalidJson);
                }

                using var content = new MemoryStream();
                using (var writer = new Utf8JsonWriter(content))
                {
                    document.RootElement.WriteTo(writer);
                }

                return content.Length > HostConfigurationReader.MaxContentBytes
                    ? HostConfigurationSnapshot.TooLarge
                    : HostConfigurationSnapshot.Available(content.ToArray());
            }
            catch (HostConfigurationFileException exception)
            {
                return exception.Reason == HostConfigurationFailureReason.FileNotFound
                    ? HostConfigurationSnapshot.Missing
                    : HostConfigurationSnapshot.Failure(exception.Reason);
            }
            catch (HostConfigurationTooLargeException)
            {
                return HostConfigurationSnapshot.TooLarge;
            }
            catch (DecoderFallbackException)
            {
                return HostConfigurationSnapshot.Failure(HostConfigurationFailureReason.InvalidUtf8);
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
    }
}
