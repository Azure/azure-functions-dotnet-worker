// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
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

        internal virtual async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsBlobExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }

            try
            {
                Dictionary<string, string> content = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingDataAsync(content, context.TargetType, modelBindingData);

                if (result is not null)
                {
                    return ConversionResult.Success(result);
                }
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }

            return ConversionResult.Unhandled();
        }

        internal virtual async ValueTask<ConversionResult> ConvertFromCollectionBindingDataAsync(ConverterContext context, CollectionModelBindingData collectionModelBindingData)
        {
            var blobCollection = new List<object>(collectionModelBindingData.ModelBindingDataArray.Length);
            Type elementType = context.TargetType.IsArray ? context.TargetType.GetElementType() : context.TargetType.GenericTypeArguments[0];

            try
            {
                foreach (ModelBindingData modelBindingData in collectionModelBindingData.ModelBindingDataArray)
                {
                    if (!IsBlobExtension(modelBindingData))
                    {
                        return ConversionResult.Unhandled();
                    }

                    Dictionary<string, string> content = GetBindingDataContent(modelBindingData);
                    var element = await ConvertModelBindingDataAsync(content, elementType, modelBindingData);

                    if (element is not null)
                    {
                        blobCollection.Add(element);
                    }
                }

                var methodName = context.TargetType.IsArray ? nameof(CloneToArray) : nameof(CloneToList);
                var result = ToTargetTypeCollection(blobCollection, methodName, elementType);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        internal bool IsBlobExtension(ModelBindingData bindingData)
        {
            if (bindingData?.Source is not Constants.BlobExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(BlobStorageConverter));
                return false;
            }

            return true;
        }

        internal Dictionary<string, string> GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, string>(bindingData?.Content?.ToObjectFromJson<Dictionary<string, string>>(), StringComparer.OrdinalIgnoreCase),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only {Constants.JsonContentType} is supported.")
            };
        }

        internal virtual async Task<object?> ConvertModelBindingDataAsync(IDictionary<string, string> content, Type targetType, ModelBindingData bindingData)
        {
            content.TryGetValue(Constants.Connection, out var connectionName);
            content.TryGetValue(Constants.ContainerName, out var containerName);
            content.TryGetValue(Constants.BlobName, out var blobName);

            if (string.IsNullOrEmpty(connectionName) || string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("'Connection' and 'ContainerName' cannot be null or empty");
            }

            return await ToTargetTypeAsync(targetType, connectionName, containerName, blobName);
        }

        internal virtual async Task<object?> ToTargetTypeAsync(Type targetType, string connectionName, string containerName, string blobName) => targetType switch
        {
            Type _ when targetType == typeof(String) => await GetBlobStringAsync(connectionName, containerName, blobName),
            Type _ when targetType == typeof(Stream) => await GetBlobStreamAsync(connectionName, containerName, blobName),
            Type _ when targetType == typeof(Byte[]) => await GetBlobBinaryDataAsync(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobBaseClient) => CreateBlobClient<BlobBaseClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobClient) => CreateBlobClient<BlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlockBlobClient) => CreateBlobClient<BlockBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(PageBlobClient) => CreateBlobClient<PageBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(AppendBlobClient) => CreateBlobClient<AppendBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobContainerClient) => CreateBlobContainerClient(connectionName, containerName),
            _ => await DeserializeToTargetObjectAsync(targetType, connectionName, containerName, blobName)
        };

        internal async Task<object?> DeserializeToTargetObjectAsync(Type targetType, string connectionName, string containerName, string blobName)
        {
            var content = await GetBlobStreamAsync(connectionName, containerName, blobName);
            return _workerOptions?.Value?.Serializer?.Deserialize(content, targetType, CancellationToken.None);
        }

        internal object? ToTargetTypeCollection(IEnumerable<object> blobCollection, string methodName, Type type)
        {
            blobCollection = blobCollection.Select(b => Convert.ChangeType(b, type));
            MethodInfo method = typeof(BlobStorageConverter).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo genericMethod = method.MakeGenericMethod(type);

            return genericMethod.Invoke(null, new[] { blobCollection.ToList() });
        }

        internal static T[] CloneToArray<T>(IList<object> source)
        {
            return source.Cast<T>().ToArray();
        }

        internal static IEnumerable<T> CloneToList<T>(IList<object> source)
        {
            return source.Cast<T>();
        }

        internal virtual async Task<string> GetBlobStringAsync(string connectionName, string containerName, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            return await GetBlobContentStringAsync(client);
        }

        private async Task<string> GetBlobContentStringAsync(BlobClient client)
        {
            var download = await client.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        internal virtual async Task<Byte[]> GetBlobBinaryDataAsync(string connectionName, string containerName, string blobName)
        {
            using MemoryStream stream = new();
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            await client.DownloadToAsync(stream);
            return stream.ToArray();
        }

        internal virtual async Task<Stream> GetBlobStreamAsync(string connectionName, string containerName, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            var download = await client.DownloadStreamingAsync();
            return download.Value.Content;
        }

        internal virtual BlobContainerClient CreateBlobContainerClient(string connectionName, string containerName)
        {
            var blobStorageOptions = _blobOptions.Get(connectionName);
            BlobServiceClient blobServiceClient = blobStorageOptions.CreateClient();
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
            return container;
        }

        internal virtual T CreateBlobClient<T>(string connectionName, string containerName, string blobName) where T : BlobBaseClient
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
