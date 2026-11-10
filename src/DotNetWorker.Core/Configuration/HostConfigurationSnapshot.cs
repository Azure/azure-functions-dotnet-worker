// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    internal sealed class HostConfigurationSnapshot
    {
        private HostConfigurationSnapshot(HostConfigurationStatus status, ReadOnlyMemory<byte> content, HostConfigurationFailureReason reason = HostConfigurationFailureReason.None)
        {
            Status = status;
            Content = content;
            Reason = reason;
        }

        public HostConfigurationStatus Status { get; }

        public ReadOnlyMemory<byte> Content { get; }

        public HostConfigurationFailureReason Reason { get; }

        public static HostConfigurationSnapshot Missing { get; } = new(HostConfigurationStatus.Missing, ReadOnlyMemory<byte>.Empty);

        public static HostConfigurationSnapshot TooLarge { get; } = new(HostConfigurationStatus.TooLarge, ReadOnlyMemory<byte>.Empty, HostConfigurationFailureReason.Oversized);

        public static HostConfigurationSnapshot Failure(HostConfigurationFailureReason reason) => new(HostConfigurationStatus.Error, ReadOnlyMemory<byte>.Empty, reason);

        public static HostConfigurationSnapshot Available(ReadOnlyMemory<byte> content) => new(HostConfigurationStatus.Available, content);
    }
}
