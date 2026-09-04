// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.Testing;

/// <summary>Creates worker-protocol input and binding data for synthetic Blob trigger invocations.</summary>
public static class BlobTriggerTestData
{
    private const string BindingSource = "AzureStorageBlobs";
    private const string JsonContentType = "application/json";

    /// <summary>Creates a trigger value for byte-array, stream, string, binary-data, or POCO binding.</summary>
    public static FunctionTestValue Content(BinaryData content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return FunctionTestValue.Bytes(content.ToMemory());
    }

    /// <summary>Creates the model-binding payload used for Blob SDK client parameters.</summary>
    public static FunctionTestValue Client(string connection, string containerName, string blobName)
    {
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new ArgumentException("A connection setting name is required.", nameof(connection));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("A container name is required.", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("A blob name is required.", nameof(blobName));
        }

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Connection = connection,
            ContainerName = containerName,
            BlobName = blobName
        });
        return new FunctionTestValue.ModelBindingValue(
            new FunctionTestModelBindingData("1.0", BindingSource, JsonContentType, content));
    }

    /// <summary>Adds common host-supplied Blob trigger binding data.</summary>
    public static FunctionInvocationRequest WithBlobMetadata(
        this FunctionInvocationRequest request,
        string blobName,
        Uri uri,
        BlobProperties? properties = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("A blob name is required.", nameof(blobName));
        }

        ArgumentNullException.ThrowIfNull(uri);
        request = request
            .WithBindingData("BlobTrigger", FunctionTestValue.String(uri.AbsoluteUri))
            .WithBindingData("Uri", FunctionTestValue.String(uri.AbsoluteUri))
            .WithBindingData("name", FunctionTestValue.String(blobName));

        if (properties is not null)
        {
            request = request
                .WithBindingData("Length", FunctionTestValue.Int64(properties.ContentLength))
                .WithBindingData("ETag", FunctionTestValue.String(properties.ETag.ToString()))
                .WithBindingData("ContentType", FunctionTestValue.String(properties.ContentType ?? string.Empty));
        }

        return request;
    }
}
