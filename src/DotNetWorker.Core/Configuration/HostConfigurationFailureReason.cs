// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    // Content-free diagnostic reasons logged when a failure is cached; never carry paths or content.
    internal enum HostConfigurationFailureReason
    {
        None = 0,
        AccessDenied,
        OpenFailed,
        ReadFailed,
        InvalidUtf8,
        InvalidJson,
        Oversized,
        FileNotFound,
    }
}
