// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Converters;
using System.Linq;
using Microsoft.Extensions.Options;
using System.Threading;
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
            if (context.Source is ModelBindingData bindingData)
            {
                if (!IsBlobExtension(bindingData))
                {
                    return ConversionResult.Unhandled();
                }

                if (!TryGetBindingDataContent(bindingData, out IDictionary<string, string> content))
                {
                    _logger.LogWarning("Unable to parse model binding data content");
                    return ConversionResult.Failed();
                }

                var result = await ConvertModelBindingDataAsync(content, context.TargetType, bindingData);

                if (result is not null)
                {
                    return ConversionResult.Success(result);
                }
            }

            if (context.Source is CollectionModelBindingData collectionBindingData)
            {
                if (!IsSupportedEnumerable(context.TargetType))
                {
                    _logger.LogWarning("Target type '{targetType}' is not supported", context.TargetType);
                    return ConversionResult.Failed();
                }

                Type individualTargetType = context.TargetType.GenericTypeArguments.FirstOrDefault();

                var collectionBlob = new List<object>(collectionBindingData.ModelBindingDataArray.Length);

                foreach (ModelBindingData modelBindingData in collectionBindingData.ModelBindingDataArray)
                {
                    if (!IsBlobExtension(modelBindingData))
                    {
                        return ConversionResult.Unhandled();
                    }

                    if (!TryGetBindingDataContent(modelBindingData, out IDictionary<string, string> content))
                    {
                        _logger.LogWarning("Unable to parse model binding data content");
                        return ConversionResult.Failed();
                    }

                    var element = await ConvertModelBindingDataAsync(content, individualTargetType, modelBindingData);

                    if (element is not null)
                    {
                        collectionBlob.Add(element);
                    }
                }

                var collectionResult = ToTargetTypeCollection(individualTargetType, collectionBlob);

                if (collectionResult is not null && collectionResult.Any())
                {
                    return ConversionResult.Success(collectionResult);
                }
            }

            return ConversionResult.Unhandled();
        }

        private bool IsBlobExtension(ModelBindingData bindingData)
        {
            if (bindingData.Source is not Constants.BlobExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData.Source, nameof(BlobStorageConverter));
                return false;
            }

            return true;
        }

        private bool TryGetBindingDataContent(ModelBindingData bindingData, out IDictionary<string, string> bindingDataContent)
        {
            bindingDataContent = bindingData.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, string>(bindingData.Content.ToObjectFromJson<Dictionary<string, string>>(), StringComparer.OrdinalIgnoreCase),
                _ => null
            };

            return bindingDataContent is not null;
        }

        private async Task<object?> ConvertModelBindingDataAsync(IDictionary<string, string> content, Type targetType, ModelBindingData bindingData)
        {
            content.TryGetValue(Constants.Connection, out var connectionName);
            content.TryGetValue(Constants.ContainerName, out var containerName);
            content.TryGetValue(Constants.BlobName, out var blobName);

            if (string.IsNullOrEmpty(connectionName) || string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("'Connection' or 'ContainerName' cannot be null or empty");
            }

            var result = await ToTargetType(targetType, connectionName, containerName, blobName);
            return result;
        }

        private async Task<object?> ToTargetType(Type targetType, string connectionName, string containerName, string blobName) => targetType switch
        {
            Type _ when targetType == typeof(String) => await GetBlobString(connectionName, containerName, blobName),
            Type _ when targetType == typeof(Stream) => await GetBlobStream(connectionName, containerName, blobName),
            Type _ when targetType == typeof(Byte[]) => await GetBlobBinaryData(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobBaseClient) => CreateBlobClient<BlobBaseClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobClient) => CreateBlobClient<BlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlockBlobClient) => CreateBlobClient<BlockBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(PageBlobClient) => CreateBlobClient<PageBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(AppendBlobClient) => CreateBlobClient<AppendBlobClient>(connectionName, containerName, blobName),
            Type _ when targetType == typeof(BlobContainerClient) => CreateBlobContainerClient(connectionName, containerName),
            _ => await DeserializeToTargetObject(targetType, connectionName, containerName, blobName)
        };

        private IEnumerable<object> ToTargetTypeCollection(Type targetType, IEnumerable<object> blobCollection) => targetType switch
        {
            Type _ when targetType == typeof(BlobBaseClient) => blobCollection.Select(b => (BlobBaseClient)b),
            Type _ when targetType == typeof(String) => blobCollection.Select(async (b) => await GetBlobContentString((BlobClient)b)),
            _ => throw new InvalidOperationException($"Requested type '{targetType}' not supported.")
        };

        private async Task<object?> DeserializeToTargetObject(Type targetType, string connectionName, string containerName, string blobName)
        {
            var content = await GetBlobStream(connectionName, containerName, blobName);
            return _workerOptions.Value.Serializer.Deserialize(content, targetType, CancellationToken.None);
        }

        private async Task<string> GetBlobString(string connectionName, string containerName, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            return await GetBlobContentString(client);
        }

        private async Task<string> GetBlobContentString(BlobClient client)
        {
            var download = await client.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private async Task<Byte[]> GetBlobBinaryData(string connectionName, string containerName, string blobName)
        {
            using MemoryStream stream = new();
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            await client.DownloadToAsync(stream);
            return stream.ToArray();
        }

        private async Task<Stream> GetBlobStream(string connectionName, string containerName, string blobName)
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

        internal static bool IsSupportedEnumerable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }
    }
}
