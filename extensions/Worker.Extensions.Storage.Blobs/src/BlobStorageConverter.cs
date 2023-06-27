// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text.Json;
using System.Globalization;
using Azure.Storage.Blobs.Models;
using Azure;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    internal class BlobStorageConverter : IInputConverter
    {
        private readonly IOptions<WorkerOptions> _workerOptions;
        private readonly IOptionsSnapshot<BlobStorageBindingOptions> _blobOptions;
        private readonly ILogger<BlobStorageConverter> _logger;

        public BlobStorageConverter(IOptions<WorkerOptions> workerOptions, IOptionsSnapshot<BlobStorageBindingOptions> blobOptions, ILogger<BlobStorageConverter> logger)
        {
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _blobOptions = blobOptions ?? throw new ArgumentNullException(nameof(blobOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => await ConvertFromBindingDataAsync(context, binding),
                _ => ConversionResult.Unhandled(),
            };
        }

        private async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            try
            {
                if (modelBindingData.Source is not Constants.BlobExtensionName)
                {
                    throw new InvalidBindingSourceException(Constants.BlobExtensionName);
                }

                BlobBindingData blobData = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingDataAsync(context.TargetType, blobData);

                return ConversionResult.Success(result);
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses JSON serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the blob to be valid json.");

                return ConversionResult.Failed(new InvalidOperationException(msg, ex));
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private BlobBindingData GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => bindingData?.Content?.ToObjectFromJson<BlobBindingData>(),
                _ => throw new InvalidContentTypeException(Constants.JsonContentType)
            };
        }

        private async Task<object?> ConvertModelBindingDataAsync(Type targetType, BlobBindingData blobData)
        {
            if (string.IsNullOrEmpty(blobData.Connection))
            {
                throw new ArgumentNullException(nameof(blobData.Connection));
            }

            if (string.IsNullOrEmpty(blobData.ContainerName))
            {
                throw new ArgumentNullException(nameof(blobData.ContainerName));
            }

            BlobContainerClient container = CreateBlobContainerClient(blobData.Connection!, blobData.ContainerName!);

            if (IsCollectionBinding(targetType, blobData.BlobName!))
            {
                return await BindToCollectionAsync(targetType, container);
            }
            else
            {
                if (targetType == typeof(BlobContainerClient) && !string.IsNullOrEmpty(blobData.BlobName))
                {
                    throw new InvalidOperationException("Binding to BlobContainerClient with a blob path is not supported. "
                                                        + "Either bind to the container path, or use BlobClient instead.");
                }

                return await ToTargetTypeAsync(targetType, container, blobData.BlobName!);
            }
        }

        private bool IsCollectionBinding(Type type, string blobName)
        {
            if (type == typeof(string) || type == typeof(byte[]))
            {
                return false;
            }

            bool isContainer = string.IsNullOrEmpty(blobName);
            bool isCollectionType = type.IsArray || typeof(IEnumerable).IsAssignableFrom(type);

            if (!isCollectionType)
            {
                return false;
            }

            if (!isContainer)
            {
                throw new InvalidOperationException("Collections are not supported when binding to a specific blob file.");
            }

            return true;
        }

        private async Task<object> BindToCollectionAsync(Type targetType, BlobContainerClient container)
        {
            Type elementType = targetType.IsArray ? targetType.GetElementType() : targetType.GenericTypeArguments[0];

            if (elementType == typeof(BlobContainerClient))
            {
                throw new InvalidOperationException("Collections of BlobContainerClient are not supported.");
            }

            var blobCount = 0;
            var resultType = typeof(List<>).MakeGenericType(elementType);
            var result = (IList)Activator.CreateInstance(resultType);

            AsyncPageable<BlobItem> resultSegment = container.GetBlobsAsync();

            await foreach (BlobItem blobItem in resultSegment)
            {
                var element = await ToTargetTypeAsync(elementType, container, blobItem.Name);

                if (element is not null)
                {
                    result.Add(element);
                    blobCount++;
                }
            }

            if (targetType.IsArray)
            {
                var arrayResult = Array.CreateInstance(elementType, blobCount);
                ((IList)result).CopyTo(arrayResult, 0);
                return arrayResult;
            }

            return result;
        }

        private async Task<object?> ToTargetTypeAsync(Type targetType, BlobContainerClient containerClient, string blobName) => targetType switch
        {
            Type _ when targetType == typeof(BlobContainerClient) => containerClient,
            Type _ when targetType == typeof(string) => await GetBlobStringAsync(containerClient, blobName),
            Type _ when targetType == typeof(Stream) => await GetBlobStreamAsync(containerClient, blobName),
            Type _ when targetType == typeof(byte[]) => await GetBlobBinaryDataAsync(containerClient, blobName),
            Type _ when targetType == typeof(BlobBaseClient) => CreateBlobClient<BlobBaseClient>(containerClient, blobName),
            Type _ when targetType == typeof(BlobClient) => CreateBlobClient<BlobClient>(containerClient, blobName),
            Type _ when targetType == typeof(BlockBlobClient) => CreateBlobClient<BlockBlobClient>(containerClient, blobName),
            Type _ when targetType == typeof(PageBlobClient) => CreateBlobClient<PageBlobClient>(containerClient, blobName),
            Type _ when targetType == typeof(AppendBlobClient) => CreateBlobClient<AppendBlobClient>(containerClient, blobName),
            _ => await DeserializeToTargetObjectAsync(targetType, containerClient, blobName)
        };

        private async Task<object?> DeserializeToTargetObjectAsync(Type targetType, BlobContainerClient containerClient, string blobName)
        {
            var content = await GetBlobStreamAsync(containerClient, blobName);
            return _workerOptions?.Value?.Serializer?.Deserialize(content, targetType, CancellationToken.None);
        }

        private async Task<string> GetBlobStringAsync(BlobContainerClient containerClient, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(containerClient, blobName);
            var download = await client.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private async Task<byte[]> GetBlobBinaryDataAsync(BlobContainerClient containerClient, string blobName)
        {
            using MemoryStream stream = new();
            var client = CreateBlobClient<BlobClient>(containerClient, blobName);
            var res = await client.DownloadToAsync(stream);
            return stream.ToArray();
        }

        private async Task<Stream> GetBlobStreamAsync(BlobContainerClient containerClient, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(containerClient, blobName);
            var download = await client.DownloadStreamingAsync();
            return download.Value.Content;
        }

        private BlobContainerClient CreateBlobContainerClient(string connectionName, string containerName)
        {
            var blobStorageOptions = _blobOptions.Get(connectionName);
            BlobServiceClient blobServiceClient = blobStorageOptions.CreateClient();
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
            return container;
        }

        private T CreateBlobClient<T>(BlobContainerClient containerClient, string blobName) where T : BlobBaseClient
        {
            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            Type targetType = typeof(T);
            BlobBaseClient blobClient = targetType switch
            {
                Type _ when targetType == typeof(BlobClient) => containerClient.GetBlobClient(blobName),
                Type _ when targetType == typeof(BlockBlobClient) => containerClient.GetBlockBlobClient(blobName),
                Type _ when targetType == typeof(PageBlobClient) => containerClient.GetPageBlobClient(blobName),
                Type _ when targetType == typeof(AppendBlobClient) => containerClient.GetAppendBlobClient(blobName),
                _ => containerClient.GetBlobBaseClient(blobName)
            };

            return (T)blobClient;
        }

        private class BlobBindingData()
        {
            public string? Connection { get; set; }
            public string? ContainerName { get; set; }
            public string? BlobName { get; set; }
        }
    }
}
