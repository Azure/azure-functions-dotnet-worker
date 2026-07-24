// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.TypeConverters
{
    /// <summary>
    /// Converter to bind the blob client family (<see cref="BlobBaseClient"/>, <see cref="BlobClient"/>,
    /// <see cref="BlockBlobClient"/>, <see cref="PageBlobClient"/> and <see cref="AppendBlobClient"/>).
    /// A single converter covers all of them because <see cref="SupportedTargetTypeAttribute"/> allows
    /// multiple declarations and the client creation logic is shared.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(BlobBaseClient))]
    [SupportedTargetType(typeof(BlobClient))]
    [SupportedTargetType(typeof(BlockBlobClient))]
    [SupportedTargetType(typeof(PageBlobClient))]
    [SupportedTargetType(typeof(AppendBlobClient))]
    internal sealed class BlobClientConverter : BlobConverterBase<BlobBaseClient>
    {
        public BlobClientConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<BlobStorageBindingOptions> blobOptions, ILogger<BlobClientConverter> logger)
            : base(workerOptions, blobOptions, logger)
        {
        }

        public override ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                if (!CanConvert(context))
                {
                    return new(ConversionResult.Unhandled());
                }

                Type targetType = context.TargetType;
                if (targetType != typeof(BlobBaseClient)
                    && targetType != typeof(BlobClient)
                    && targetType != typeof(BlockBlobClient)
                    && targetType != typeof(PageBlobClient)
                    && targetType != typeof(AppendBlobClient))
                {
                    return new(ConversionResult.Unhandled());
                }

                var blobData = GetBindingDataContent((ModelBindingData)context.Source!);
                BlobContainerClient container = GetContainerClient(blobData);

                if (string.IsNullOrEmpty(blobData.BlobName))
                {
                    throw new InvalidOperationException($"'{nameof(blobData.BlobName)}' cannot be null or empty when binding to a single blob.");
                }

                BlobBaseClient result = targetType switch
                {
                    Type _ when targetType == typeof(BlobClient) => CreateBlobClient<BlobClient>(container, blobData.BlobName!),
                    Type _ when targetType == typeof(BlockBlobClient) => CreateBlobClient<BlockBlobClient>(container, blobData.BlobName!),
                    Type _ when targetType == typeof(PageBlobClient) => CreateBlobClient<PageBlobClient>(container, blobData.BlobName!),
                    Type _ when targetType == typeof(AppendBlobClient) => CreateBlobClient<AppendBlobClient>(container, blobData.BlobName!),
                    _ => CreateBlobClient<BlobBaseClient>(container, blobData.BlobName!)
                };

                return new(ToConversionResult(result, targetType));
            }
            catch (JsonException ex)
            {
                return new(ConversionResult.Failed(CreateComplexObjectFailure(ex)));
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }
        }
    }
}
