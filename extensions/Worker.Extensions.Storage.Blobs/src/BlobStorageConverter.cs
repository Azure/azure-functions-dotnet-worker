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
                CollectionModelBindingData binding => await ConvertFromCollectionBindingDataAsync(context, binding),
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

                Dictionary<string, string> content = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingDataAsync(content, context.TargetType, modelBindingData);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private async ValueTask<ConversionResult> ConvertFromCollectionBindingDataAsync(ConverterContext context, CollectionModelBindingData collectionModelBindingData)
        {
            Type elementType = context.TargetType.IsArray
                ? context.TargetType.GetElementType()
                : context.TargetType.GenericTypeArguments[0];

            IList result = Array.CreateInstance(elementType, collectionModelBindingData.ModelBindingDataArray.Length);

            try
            {
                for (var i = 0; i < collectionModelBindingData.ModelBindingDataArray.Length; i++)
                {
                    var modelBindingData = collectionModelBindingData.ModelBindingDataArray[i];

                    if (modelBindingData.Source is not Constants.BlobExtensionName)
                    {
                        throw new InvalidBindingSourceException(Constants.BlobExtensionName);
                    }

                    Dictionary<string, string> content = GetBindingDataContent(modelBindingData);
                    var element = await ConvertModelBindingDataAsync(content, elementType, modelBindingData);

                    if (element is not null)
                    {
                        result[i] = element;
                    }
                }

                if (!context.TargetType.IsArray)
                {
                    var resultType = typeof(List<>).MakeGenericType(elementType);
                    result = (IList)Activator.CreateInstance(resultType, result);
                }

                return ConversionResult.Success(result);
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses JSON serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the blob to be valid json.
                    The JSON parser failed: {0}",
                    ex.Message);

                return ConversionResult.Failed(new InvalidOperationException(msg, ex));
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private Dictionary<string, string> GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, string>(bindingData?.Content?.ToObjectFromJson<Dictionary<string, string>>(), StringComparer.OrdinalIgnoreCase),
                _ => throw new InvalidContentTypeException(Constants.JsonContentType)
            };
        }

        private async Task<object?> ConvertModelBindingDataAsync(IDictionary<string, string> content, Type targetType, ModelBindingData bindingData)
        {
            content.TryGetValue(Constants.Connection, out var connectionName);
            content.TryGetValue(Constants.ContainerName, out var containerName);
            content.TryGetValue(Constants.BlobName, out var blobName);

            if (string.IsNullOrEmpty(connectionName))
            {
                throw new ArgumentNullException(nameof(connectionName));
            }

            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException(nameof(containerName));
            }

            return await ToTargetTypeAsync(targetType, connectionName, containerName, blobName);
        }

        private async Task<object?> ToTargetTypeAsync(Type targetType, string connectionName, string containerName, string blobName) => targetType switch
        {
            Type _ when targetType == typeof(string) => await GetBlobStringAsync(connectionName, containerName, blobName),
            Type _ when targetType == typeof(Stream) => await GetBlobStreamAsync(connectionName, containerName, blobName),
            Type _ when targetType == typeof(byte[]) => await GetBlobBinaryDataAsync(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobBaseClient) => CreateBlobClient<BlobBaseClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobClient) => CreateBlobClient<BlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlockBlobClient) => CreateBlobClient<BlockBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(PageBlobClient) => CreateBlobClient<PageBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(AppendBlobClient) => CreateBlobClient<AppendBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobContainerClient) => CreateBlobContainerClient(connectionName, containerName),
            _ => await DeserializeToTargetObjectAsync(targetType, connectionName, containerName, blobName)
        };

        private async Task<object?> DeserializeToTargetObjectAsync(Type targetType, string connectionName, string containerName, string blobName)
        {
            var content = await GetBlobStreamAsync(connectionName, containerName, blobName);
            return _workerOptions?.Value?.Serializer?.Deserialize(content, targetType, CancellationToken.None);
        }

        private async Task<string> GetBlobStringAsync(string connectionName, string containerName, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            var download = await client.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private async Task<byte[]> GetBlobBinaryDataAsync(string connectionName, string containerName, string blobName)
        {
            using MemoryStream stream = new();
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            var res = await client.DownloadToAsync(stream);
            return stream.ToArray();
        }

        private async Task<Stream> GetBlobStreamAsync(string connectionName, string containerName, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
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

        private T CreateBlobClient<T>(string connectionName, string containerName, string blobName) where T : BlobBaseClient
        {
            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            BlobContainerClient container = CreateBlobContainerClient(connectionName, containerName);

            Type targetType = typeof(T);
            BlobBaseClient blobClient = targetType switch
            {
                Type _ when targetType == typeof(BlobClient) => container.GetBlobClient(blobName),
                Type _ when targetType == typeof(BlockBlobClient) => container.GetBlockBlobClient(blobName),
                Type _ when targetType == typeof(PageBlobClient) => container.GetPageBlobClient(blobName),
                Type _ when targetType == typeof(AppendBlobClient) => container.GetAppendBlobClient(blobName),
                _ => container.GetBlobBaseClient(blobName)
            };

            return (T)blobClient;
        }
    }
}
