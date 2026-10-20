// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
    /// Converter to bind <see cref="BlobContainerClient" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(BlobContainerClient))]
    internal sealed class BlobContainerClientConverter : BlobConverterBase<BlobContainerClient>
    {
        public BlobContainerClientConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<BlobStorageBindingOptions> blobOptions, ILogger<BlobContainerClientConverter> logger)
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

                if (context.TargetType != typeof(BlobContainerClient))
                {
                    return new(ConversionResult.Unhandled());
                }

                var blobData = GetBindingDataContent((ModelBindingData)context.Source!);
                BlobContainerClient container = GetContainerClient(blobData);

                if (!string.IsNullOrEmpty(blobData.BlobName) && BlobIsFileRegex.IsMatch(blobData.BlobName))
                {
                    throw new InvalidOperationException("Binding to a BlobContainerClient with a blob path is not supported. "
                                                        + "Either bind to the container path, or use BlobClient instead.");
                }

                return new(ToConversionResult(container, context.TargetType));
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
