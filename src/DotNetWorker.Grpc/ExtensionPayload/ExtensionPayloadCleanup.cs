// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal static class ExtensionPayloadCleanup
{
    internal static Exception? Dispose(IDisposable? resource, Exception? failure)
    {
        try
        {
            resource?.Dispose();
        }
        catch (Exception exception)
        {
            return failure is null ? exception : new AggregateException(failure, exception);
        }

        return failure;
    }
}
