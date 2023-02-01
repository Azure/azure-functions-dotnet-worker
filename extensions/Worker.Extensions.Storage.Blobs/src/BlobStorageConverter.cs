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
            if (context.Source is ModelBindingData bindingData)
            {
                if (!IsBlobExtension(bindingData))
                {
                    return ConversionResult.Unhandled();
                }

                if (!TryGetBindingDataContent(bindingData, out IDictionary<string, string> content))
                {
                    return ConversionResult.Failed(new InvalidOperationException("Unable to parse model binding data content"));
                }

                var result = await ConvertModelBindingDataAsync(content, context.TargetType, bindingData);

                if (result is not null)
                {
                    return ConversionResult.Success(result);
                }
            }

            if (context.Source is CollectionModelBindingData collectionBindingData)
            {
                var collectionBlob = new List<object>(collectionBindingData.ModelBindingDataArray.Length);

                foreach (ModelBindingData modelBindingData in collectionBindingData.ModelBindingDataArray)
                {
                    if (!IsBlobExtension(modelBindingData))
                    {
                        return ConversionResult.Unhandled();
                    }

                    if (!TryGetBindingDataContent(modelBindingData, out IDictionary<string, string> content))
                    {
                        return ConversionResult.Failed(new InvalidOperationException("Unable to parse model binding data content"));
                    }

                    var element = await ConvertModelBindingDataAsync(
                        content,
                        context.TargetType.IsArray ? context.TargetType.GetElementType() : context.TargetType.GenericTypeArguments[0],
                        modelBindingData);

                    if (element is not null)
                    {
                        collectionBlob.Add(element);
                    }
                }

                var collectionResult = context.TargetType.IsArray
                                    ? ToTargetTypeArray(context.TargetType, collectionBlob)
                                    : ToTargetTypeCollection(context.TargetType, collectionBlob);

                if (collectionResult is null && collectionBlob.Any())
                {
                    var result = DeserializeToTargetObjectCollection(collectionBlob, context.TargetType);
                    if (result is not null)
                    {
                        return ConversionResult.Success(result);
                    }
                }

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

            return await ToTargetTypeAsync(targetType, connectionName, containerName, blobName);
        }

        private async Task<object?> ToTargetTypeAsync(Type targetType, string connectionName, string containerName, string blobName) => targetType switch
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

        private object[]? ToTargetTypeArray(Type targetType, IEnumerable<object> blobCollection) => targetType switch
        {
            Type _ when targetType == typeof(String[]) => blobCollection.Select(b => (string)b).ToArray(),
            Type _ when targetType == typeof(Stream[]) => blobCollection.Select(b => (Stream)b).ToArray(),
            Type _ when targetType == typeof(Byte[][]) => blobCollection.Select(b => (Byte[])b).ToArray(),
            Type _ when targetType == typeof(BlobBaseClient[]) => blobCollection.Select(b => (BlobBaseClient)b).ToArray(),
            Type _ when targetType == typeof(BlobClient[]) => blobCollection.Select(b => (BlobClient)b).ToArray(),
            Type _ when targetType == typeof(BlockBlobClient[]) => blobCollection.Select(b => (BlockBlobClient)b).ToArray(),
            Type _ when targetType == typeof(PageBlobClient[]) => blobCollection.Select(b => (PageBlobClient)b).ToArray(),
            Type _ when targetType == typeof(AppendBlobClient[]) => blobCollection.Select(b => (AppendBlobClient)b).ToArray(),
            Type _ when targetType == typeof(BlobContainerClient) => blobCollection.Select(b => (BlobClient)b).ToArray(),
            _ => null
        };

        private IEnumerable<object>? ToTargetTypeCollection(Type targetType, IEnumerable<object> blobCollection) => targetType switch
        {
            Type _ when targetType == typeof(IEnumerable<String>) || targetType == typeof(String[]) => blobCollection.Select(b => (string)b),
            Type _ when targetType == typeof(IEnumerable<Stream>) => blobCollection.Select(b => (Stream)b),
            Type _ when targetType == typeof(IEnumerable<Byte[]>) => blobCollection.Select(b => (Byte[])b),
            Type _ when targetType == typeof(IEnumerable<BlobBaseClient>) => blobCollection.Select(b => (BlobBaseClient)b),
            Type _ when targetType == typeof(IEnumerable<BlobClient>) => blobCollection.Select(b => (BlobClient)b),
            Type _ when targetType == typeof(IEnumerable<BlockBlobClient>) => blobCollection.Select(b => (BlockBlobClient)b),
            Type _ when targetType == typeof(IEnumerable<PageBlobClient>) => blobCollection.Select(b => (PageBlobClient)b),
            Type _ when targetType == typeof(IEnumerable<AppendBlobClient>) => blobCollection.Select(b => (AppendBlobClient)b),
            Type _ when targetType == typeof(IEnumerable<BlobContainerClient>) => blobCollection.Select(b => (BlobClient)b),
            _ => null
        };

        private async Task<object?> DeserializeToTargetObjectAsync(Type targetType, string connectionName, string containerName, string blobName)
        {
            var content = await GetBlobStreamAsync(connectionName, containerName, blobName);
            return _workerOptions.Value.Serializer.Deserialize(content, targetType, CancellationToken.None);
        }

        private object? DeserializeToTargetObjectCollection(IEnumerable<object> blobCollection, Type targetType)
        {
            (string methodName, Type type) = targetType.IsArray
                                            ? (nameof(CloneToArray), targetType.GetElementType())
                                            : (nameof(CloneToList), targetType.GenericTypeArguments[0]);

            blobCollection = blobCollection.Select(b => Convert.ChangeType(b, type));
            MethodInfo method = typeof(BlobStorageConverter).GetMethod(methodName);
            MethodInfo genericMethod = method.MakeGenericMethod(type);

            return genericMethod.Invoke(null, new[] { blobCollection.ToList() });
        }

        public static T[] CloneToArray<T>(IList<object> source)
        {
            return source.Cast<T>().ToArray();
        }

        public static IEnumerable<T> CloneToList<T>(IList<object> source)
        {
            return source.Cast<T>().ToList();
        }

        private async Task<string> GetBlobStringAsync(string connectionName, string containerName, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            return await GetBlobContentStringAsync(client);
        }

        private async Task<string> GetBlobContentStringAsync(BlobClient client)
        {
            var download = await client.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private async Task<Byte[]> GetBlobBinaryDataAsync(string connectionName, string containerName, string blobName)
        {
            using MemoryStream stream = new();
            var client = CreateBlobClient<BlobClient>(connectionName, containerName, blobName);
            await client.DownloadToAsync(stream);
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