// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.TypeConverters
{
    /// <summary>
    /// Converter to bind collections (<see cref="IEnumerable{T}"/> and arrays) by listing the blobs under a
    /// container/prefix and binding each element. Blob collections are heterogeneous (for example
    /// <c>List&lt;string&gt;</c>, <c>BlobClient[]</c> or <c>List&lt;MyPoco&gt;</c>), so this converter keeps a small
    /// per-element dispatch that reuses the shared base helpers. It carries no
    /// <see cref="SupportedTargetTypeAttribute"/> and therefore must be registered before <see cref="BlobPocoConverter"/>.
    /// </summary>
    [SupportsDeferredBinding]
    internal sealed class BlobCollectionConverter : BlobConverterBase<object>
    {
        public BlobCollectionConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<BlobStorageBindingOptions> blobOptions, ILogger<BlobCollectionConverter> logger)
            : base(workerOptions, blobOptions, logger)
        {
        }

        public override async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                if (!CanConvert(context))
                {
                    return ConversionResult.Unhandled();
                }

                if (!context.TargetType.TryGetCollectionElementType(out Type? elementType) || elementType is null)
                {
                    // Not a collection target; let the scalar / POCO converters handle it.
                    return ConversionResult.Unhandled();
                }

                var blobData = GetBindingDataContent((ModelBindingData)context.Source!);
                BlobContainerClient container = GetContainerClient(blobData);

                if (elementType == typeof(BlobContainerClient))
                {
                    throw new InvalidOperationException("Binding to a BlobContainerClient collection is not supported.");
                }

                bool isFile = !string.IsNullOrEmpty(blobData.BlobName) && BlobIsFileRegex.IsMatch(blobData.BlobName);

                if (isFile && typeof(BlobBaseClient).IsAssignableFrom(elementType))
                {
                    throw new InvalidOperationException("Binding to a blob client collection with a blob path is not supported. "
                                                        + "Either bind to the container path, or use BlobClient instead.");
                }

                // A file-looking name is treated as a single blob (for example, one file containing a JSON array),
                // so defer to the POCO converter which deserializes the blob content into the collection target.
                if (isFile)
                {
                    return ConversionResult.Unhandled();
                }

                object result = await BindToCollectionAsync(context.TargetType, elementType, container, blobData.BlobName!);
                return ToConversionResult(result, context.TargetType);
            }
            catch (JsonException ex)
            {
                return ConversionResult.Failed(CreateComplexObjectFailure(ex));
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private async Task<object> BindToCollectionAsync(Type targetType, Type elementType, BlobContainerClient container, string blobPath)
        {
            var resultType = typeof(List<>).MakeGenericType(elementType);
            var result = (IList)Activator.CreateInstance(resultType)!;

            await foreach (BlobItem blobItem in container.GetBlobsAsync(prefix: blobPath).ConfigureAwait(false))
            {
                var element = await ToElementAsync(elementType, container, blobItem.Name);
                result.Add(element);
            }

            if (targetType.IsArray)
            {
                var arrayResult = Array.CreateInstance(elementType, result.Count);
                result.CopyTo(arrayResult, 0);
                return arrayResult;
            }

            return result;
        }

        // Minimal per-element dispatch reusing the base helpers. Mirrors the original ToTargetTypeAsync switch.
        private async Task<object?> ToElementAsync(Type elementType, BlobContainerClient container, string blobName) => elementType switch
        {
            Type _ when elementType == typeof(string) => await GetBlobStringAsync(container, blobName),
            Type _ when elementType == typeof(Stream) => await GetBlobStreamAsync(container, blobName),
            Type _ when elementType == typeof(byte[]) => await GetBlobBinaryDataAsync(container, blobName),
            Type _ when elementType == typeof(BlobBaseClient) => CreateBlobClient<BlobBaseClient>(container, blobName),
            Type _ when elementType == typeof(BlobClient) => CreateBlobClient<BlobClient>(container, blobName),
            Type _ when elementType == typeof(BlockBlobClient) => CreateBlobClient<BlockBlobClient>(container, blobName),
            Type _ when elementType == typeof(PageBlobClient) => CreateBlobClient<PageBlobClient>(container, blobName),
            Type _ when elementType == typeof(AppendBlobClient) => CreateBlobClient<AppendBlobClient>(container, blobName),
            _ => await DeserializeElementAsync(elementType, container, blobName)
        };

        private async Task<object?> DeserializeElementAsync(Type elementType, BlobContainerClient container, string blobName)
        {
            var content = await GetBlobStreamAsync(container, blobName);
            return DeserializeToTargetObject(content, elementType);
        }
    }
}
