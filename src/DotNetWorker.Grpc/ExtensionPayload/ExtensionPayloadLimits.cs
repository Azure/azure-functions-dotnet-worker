// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal static class ExtensionPayloadLimits
{
    internal const int MaximumChunkBytes = 64 * 1024;
    internal const long MaximumArchiveBytes = 256L * 1024 * 1024;
}
