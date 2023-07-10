// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

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
        private readonly Regex BlobIsFileRegex = new Regex(@"\.[^.\/]+$");

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
                string msg = string.Format(CultureInfo.CurrentCulture,
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
            if (bindingData is null)
            {
                throw new ArgumentNullException(nameof(bindingData));
            }

            return bindingData.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<BlobBindingData>(),
                _ => throw new InvalidContentTypeException(Constants.JsonContentType)
            };
        }

        private async Task<object?> ConvertModelBindingDataAsync(Type targetType, BlobBindingData blobData)
        {
            if (blobData is null)
            {
                throw new ArgumentNullException(nameof(blobData));
            }

            if (string.IsNullOrEmpty(blobData.Connection))
            {
                throw new InvalidOperationException($"'{nameof(blobData.Connection)}' cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(blobData.ContainerName))
            {
                throw new InvalidOperationException($"'{nameof(blobData.ContainerName)}' cannot be null or empty.");
            }

            BlobContainerClient container = CreateBlobContainerClient(blobData.Connection!, blobData.ContainerName!);

            if (IsCollectionBinding(targetType, blobData.BlobName!))
            {
                Type elementType = targetType.IsArray ? targetType.GetElementType() : targetType.GenericTypeArguments[0];

                if (elementType == typeof(BlobContainerClient))
                {
                    throw new InvalidOperationException("Binding to BlobContainerClient collection is not supported.");
                }

                if (typeof(BlobBaseClient).IsAssignableFrom(elementType) && !string.IsNullOrEmpty(blobData.BlobName) && BlobIsFileRegex.IsMatch(blobData.BlobName))
                {
                    throw new InvalidOperationException("Binding to a blob client collection with a blob path is not supported. "
                                                        + "Either bind to the container path, or use BlobClient instead.");
                }

                return await BindToCollectionAsync(targetType, elementType, container, blobData.BlobName!);
            }
            else
            {
                if (string.IsNullOrEmpty(blobData.BlobName))
                {
                    throw new InvalidOperationException($"'{nameof(blobData.BlobName)}' cannot be null or empty when binding to a single blob.");
                }

                if (targetType == typeof(BlobContainerClient))
                {
                    throw new InvalidOperationException("Binding to BlobContainerClient with a blob path is not supported. "
                                                        + "Either bind to the container path, or use BlobClient instead.");
                }

                return await ToTargetTypeAsync(targetType, container, blobData.BlobName!);
            }
        }

        /// <summary>
        /// Determines if the binding is a collection binding.
        /// A collection binding is when the target type is an array or IEnumerable and
        /// the blob name is either null or empty (meaning a container path is provided).
        /// If a blob name is provided, it must be a directory path (no file extension provided).
        /// </summary>
        private bool IsCollectionBinding(Type targetType, string blobName)
        {
            // Edge case: These two types should be treated as a single blob binding
            // string implements IEnumerable<char> and byte[] would pass the IsArray check
            if (targetType == typeof(string) || targetType == typeof(byte[]))
            {
                return false;
            }

            if (!(targetType.IsArray || typeof(IEnumerable).IsAssignableFrom(targetType)))
            {
                return false;
            }

            return true;
        }

        private async Task<object> BindToCollectionAsync(Type targetType, Type elementType, BlobContainerClient container, string blobPath)
        {
            if (BlobIsFileRegex.IsMatch(blobPath))
            {
                // Binding is to a specific blob file, deserialize the content to target type
                var deserializedResult = await DeserializeToTargetObjectAsync(targetType, container, blobPath);

                return deserializedResult ?? throw new InvalidOperationException($"Could not deserialize blob '{blobPath}' to '{targetType}'.");
            }

            var resultType = typeof(List<>).MakeGenericType(elementType);
            var result = (IList)Activator.CreateInstance(resultType);

            await foreach (BlobItem blobItem in container.GetBlobsAsync(prefix: blobPath).ConfigureAwait(false))
            {
                var element = await ToTargetTypeAsync(elementType, container, blobItem.Name);
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
