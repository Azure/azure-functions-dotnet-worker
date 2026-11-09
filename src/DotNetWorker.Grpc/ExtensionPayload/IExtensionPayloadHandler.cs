// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal interface IExtensionPayloadHandler
{
    Task SendAsync(string requestId, IWorkerClient client, CancellationToken stopping);

    /// <summary>
    /// Cancels the active transfer and returns its completion, including stream cleanup.
    /// </summary>
    /// <remarks>
    /// Cancellation callback failures must be reported through the returned task, not thrown synchronously,
    /// so the caller can initiate transport shutdown before awaiting the transfer.
    /// </remarks>
    Task CancelAsync();
}
