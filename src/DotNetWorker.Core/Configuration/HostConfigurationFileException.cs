// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    internal sealed class HostConfigurationFileException(HostConfigurationFailureReason reason, Exception innerException)
        : IOException("The host configuration file could not be opened.", innerException)
    {
        public HostConfigurationFailureReason Reason { get; } = reason;
    }
}
