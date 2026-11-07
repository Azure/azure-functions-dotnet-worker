// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.Testing;

/// <summary>Provides synthetic Blob trigger invocation helpers.</summary>
public static class BlobFunctionsApplicationFactoryExtensions
{
    /// <summary>
    /// Invokes a Blob-triggered function through the production worker pipeline.
    /// Listener polling, receipts, retries, and storage side effects are not simulated.
    /// </summary>
    public static Task<FunctionInvocationResult> InvokeBlobAsync<TEntryPoint>(
        this FunctionsApplicationFactory<TEntryPoint> factory,
        string functionName,
        string triggerParameterName,
        BinaryData content,
        string blobName,
        Uri uri,
        CancellationToken cancellationToken = default)
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithInput(triggerParameterName, BlobTriggerTestData.Content(content))
            .WithBlobMetadata(blobName, uri);
        return factory.InvokeAsync(functionName, request, cancellationToken);
    }

    /// <summary>
    /// Invokes a Blob-triggered function whose trigger parameter is a Blob SDK client.
    /// Creating the client performs no storage network operation; user code can replace or exercise it normally.
    /// </summary>
    public static Task<FunctionInvocationResult> InvokeBlobClientAsync<TEntryPoint>(
        this FunctionsApplicationFactory<TEntryPoint> factory,
        string functionName,
        string triggerParameterName,
        string connection,
        string containerName,
        string blobName,
        Uri uri,
        CancellationToken cancellationToken = default)
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithInput(triggerParameterName, BlobTriggerTestData.Client(connection, containerName, blobName))
            .WithBlobMetadata(blobName, uri);
        return factory.InvokeAsync(functionName, request, cancellationToken);
    }
}
