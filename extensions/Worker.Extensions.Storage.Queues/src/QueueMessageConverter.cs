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
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportedConverterTypes(typeof(QueueMessage), typeof(BinaryData), typeof(JObject))]
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

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            var result = context?.Source switch
            {
                ModelBindingData binding => ConvertFromBindingDataAsync(context, binding),
                _ => ConversionResult.Unhandled()
            };

            return new ValueTask<ConversionResult>(result);
        }

        private ConversionResult ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsQueueExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }

            try
            {

                QueueMessage queueMessage = ExtractQueueMessageFromBindingData(modelBindingData);
                var result = ToTargetType(context.TargetType, queueMessage);

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

        private bool IsQueueExtension(ModelBindingData bindingData)
        {
            if (bindingData?.Source is not Constants.QueueExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(QueueMessageConverter));
                return false;
            }

            return true;
        }

        private QueueMessage ExtractQueueMessageFromBindingData(ModelBindingData modelBindingData)
        {
            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.");
            }

            JsonSerializerOptions options = new() { Converters = { new QueueMessageJsonConverter() } };
            return modelBindingData.Content.ToObjectFromJson<QueueMessage>(options);
        }

        private object? ToTargetType(Type targetType, QueueMessage message) => targetType switch
        {
            Type _ when targetType == typeof(QueueMessage) => message,
            Type _ when targetType == typeof(BinaryData) => ConvertToBinaryData(message),
            Type _ when targetType == typeof(JObject) => ConvertToJObject(message),
            _ => null
        };

        private BinaryData ConvertToBinaryData(QueueMessage input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return input.Body;
        }

        private JObject ConvertToJObject(QueueMessage input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // What should this being be? The queueMessage as JObject, or the body
            // as JObject
            return JObject.FromObject(input);
        }
    }
}