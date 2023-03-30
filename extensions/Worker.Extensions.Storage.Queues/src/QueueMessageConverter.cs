// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportedConverterTypes(typeof(QueueMessage))]
    internal class QueueMessageConverter : IInputConverter
    {
        // private readonly IOptions<WorkerOptions> _workerOptions;
        // private readonly IOptionsSnapshot<BlobStorageBindingOptions> _blobOptions;

        private readonly ILogger<QueueMessageConverter> _logger;

        // IOptions<WorkerOptions> workerOptions, IOptionsSnapshot<BlobStorageBindingOptions> blobOptions,
        public QueueMessageConverter(ILogger<QueueMessageConverter> logger)
        {
            // _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            // _blobOptions = blobOptions ?? throw new ArgumentNullException(nameof(blobOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ConversionResult ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => ConvertFromBindingDataAsync(context, binding),
                _ => ConversionResult.Unhandled()
            };
        }

        private ConversionResult ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsQueueExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }

            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.");
            }

            try
            {
                var options = new JsonSerializerOptions();
                var queueMessage = modelBindingData.Content.ToObjectFromJson<QueueMessage>(options);

                if (queueMessage is not null)
                {
                    return ConversionResult.Success(queueMessage);
                }
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }

            return ConversionResult.Unhandled();

        }

        private bool IsQueueExtension(ModelBindingData bindingData)
        {
            if (bindingData?.Source is not Constants.QueueExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(QueueMessageConverter));
                return false;
            }

            return true;
        }
    }
}