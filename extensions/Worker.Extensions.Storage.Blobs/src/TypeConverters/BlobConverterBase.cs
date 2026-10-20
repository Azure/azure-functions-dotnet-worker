// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.TypeConverters
{
    /// <summary>
    /// Base type for the Blob Storage input converters. Holds the shared binding-data parsing,
    /// container/client creation, blob download helpers and validation rules so that each concrete
    /// converter only has to deal with a single target type.
    /// </summary>
    internal abstract class BlobConverterBase<T> : IInputConverter
    {
        protected readonly IOptions<WorkerOptions> WorkerOptions;
        protected readonly IOptionsMonitor<BlobStorageBindingOptions> BlobOptions;
        protected readonly ILogger<BlobConverterBase<T>> Logger;

        protected static readonly Regex BlobIsFileRegex = new(@"\.[^.\/]+$");

        protected BlobConverterBase(
            IOptions<WorkerOptions> workerOptions,
            IOptionsMonitor<BlobStorageBindingOptions> blobOptions,
            ILogger<BlobConverterBase<T>> logger)
        {
            WorkerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            BlobOptions = blobOptions ?? throw new ArgumentNullException(nameof(blobOptions));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public abstract ValueTask<ConversionResult> ConvertAsync(ConverterContext context);

        /// <summary>
        /// Source gate shared by every converter. Returns <see langword="false"/> when the source is not a
        /// <see cref="ModelBindingData"/> (so the next converter gets a chance), and throws when the binding
        /// data originates from a different extension (so the conversion fails fast).
        /// </summary>
        protected static bool CanConvert(ConverterContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Source is not ModelBindingData bindingData)
            {
                return false;
            }

            if (bindingData.Source is not Constants.BlobExtensionName)
            {
                throw new InvalidBindingSourceException(bindingData.Source, Constants.BlobExtensionName);
            }

            return true;
        }

        protected static BlobBindingData GetBindingDataContent(ModelBindingData bindingData)
        {
            if (bindingData is null)
            {
                throw new ArgumentNullException(nameof(bindingData));
            }

            return bindingData.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<BlobBindingData>()!,
                _ => throw new InvalidContentTypeException(bindingData.ContentType, Constants.JsonContentType)
            };
        }

        /// <summary>
        /// Validates the connection / container information and creates a <see cref="BlobContainerClient"/>.
        /// The exception types and messages are preserved from the original converter as tests assert on them.
        /// </summary>
        protected BlobContainerClient GetContainerClient(BlobBindingData blobData)
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

            BlobStorageBindingOptions options = BlobOptions.Get(blobData.Connection);
            BlobServiceClient blobServiceClient = options.CreateClient();
            return blobServiceClient.GetBlobContainerClient(blobData.ContainerName);
        }

        protected TClient CreateBlobClient<TClient>(BlobContainerClient containerClient, string blobName) where TClient : BlobBaseClient
        {
            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            Type targetType = typeof(TClient);
            BlobBaseClient blobClient = targetType switch
            {
                Type _ when targetType == typeof(BlobClient) => containerClient.GetBlobClient(blobName),
                Type _ when targetType == typeof(BlockBlobClient) => containerClient.GetBlockBlobClient(blobName),
                Type _ when targetType == typeof(PageBlobClient) => containerClient.GetPageBlobClient(blobName),
                Type _ when targetType == typeof(AppendBlobClient) => containerClient.GetAppendBlobClient(blobName),
                _ => containerClient.GetBlobBaseClient(blobName)
            };

            return (TClient)blobClient;
        }

        protected async Task<string> GetBlobStringAsync(BlobContainerClient containerClient, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(containerClient, blobName);
            var download = await client.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        protected async Task<byte[]> GetBlobBinaryDataAsync(BlobContainerClient containerClient, string blobName)
        {
            using MemoryStream stream = new();
            var client = CreateBlobClient<BlobClient>(containerClient, blobName);
            await client.DownloadToAsync(stream);
            return stream.ToArray();
        }

        protected async Task<Stream> GetBlobStreamAsync(BlobContainerClient containerClient, string blobName)
        {
            var client = CreateBlobClient<BlobClient>(containerClient, blobName);
            return await client.OpenReadAsync();
        }

        protected object? DeserializeToTargetObject(Stream content, Type targetType)
        {
            return WorkerOptions?.Value?.Serializer?.Deserialize(content, targetType, CancellationToken.None);
        }

        /// <summary>
        /// Wraps a converted value into a <see cref="ConversionResult"/>, preserving the original behavior of
        /// failing with the "Unable to convert..." message when the conversion yields <see langword="null"/>.
        /// </summary>
        protected static ConversionResult ToConversionResult(object? result, Type targetType)
        {
            return result is null
                ? ConversionResult.Failed(new InvalidOperationException($"Unable to convert blob binding data to type '{targetType.Name}'."))
                : ConversionResult.Success(result);
        }

        /// <summary>
        /// Builds the friendly "complex objects use JSON serialization" error preserved from the original converter.
        /// </summary>
        protected static InvalidOperationException CreateComplexObjectFailure(JsonException ex)
        {
            string msg = string.Format(CultureInfo.CurrentCulture,
                @"Binding parameters to complex objects uses JSON serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the blob to be valid json.");

            return new InvalidOperationException(msg, ex);
        }

        protected sealed class BlobBindingData
        {
            public string? Connection { get; set; }
            public string? ContainerName { get; set; }
            public string? BlobName { get; set; }
        }
    }
}
