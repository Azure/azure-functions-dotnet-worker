// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.TypeConverters
{
    /// <summary>
    /// Fallback converter that deserializes the blob content into the target type using the configured serializer.
    /// It carries no <see cref="SupportedTargetTypeAttribute"/> so it is considered for any target type and must be
    /// registered last. A file-looking collection target (for example a single file containing a JSON array) also
    /// lands here after <see cref="BlobCollectionConverter"/> defers it.
    /// </summary>
    [SupportsDeferredBinding]
    internal sealed class BlobPocoConverter : BlobConverterBase<object>
    {
        public BlobPocoConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<BlobStorageBindingOptions> blobOptions, ILogger<BlobPocoConverter> logger)
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

                var blobData = GetBindingDataContent((ModelBindingData)context.Source!);
                BlobContainerClient container = GetContainerClient(blobData);

                if (string.IsNullOrEmpty(blobData.BlobName))
                {
                    throw new InvalidOperationException($"'{nameof(blobData.BlobName)}' cannot be null or empty when binding to a single blob.");
                }

                Stream content = await GetBlobStreamAsync(container, blobData.BlobName!);
                var result = DeserializeToTargetObject(content, context.TargetType);

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
    }
}
