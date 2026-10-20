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
    /// Converter to bind <see cref="Stream" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(Stream))]
    internal sealed class BlobStreamConverter : BlobConverterBase<Stream>
    {
        public BlobStreamConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<BlobStorageBindingOptions> blobOptions, ILogger<BlobStreamConverter> logger)
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

                if (context.TargetType != typeof(Stream))
                {
                    return ConversionResult.Unhandled();
                }

                var blobData = GetBindingDataContent((ModelBindingData)context.Source!);
                BlobContainerClient container = GetContainerClient(blobData);

                if (string.IsNullOrEmpty(blobData.BlobName))
                {
                    throw new InvalidOperationException($"'{nameof(blobData.BlobName)}' cannot be null or empty when binding to a single blob.");
                }

                Stream result = await GetBlobStreamAsync(container, blobData.BlobName!);
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
