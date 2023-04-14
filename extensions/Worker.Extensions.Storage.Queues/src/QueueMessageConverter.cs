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
        private readonly ILogger<QueueMessageConverter> _logger;

        public QueueMessageConverter(ILogger<QueueMessageConverter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            var result = context?.Source switch
            {
                ModelBindingData binding => ConvertFromBindingData(context, binding),
                _ => ConversionResult.Unhandled()
            };

            return new ValueTask<ConversionResult>(result);
        }

        private ConversionResult ConvertFromBindingData(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsQueueExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }

            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                return ConversionResult.Failed(new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported."));
            }

            try
            {
                var result = ToTargetType(context.TargetType, modelBindingData);

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

        private object? ToTargetType(Type targetType, ModelBindingData modelBindingData) => targetType switch
        {
            Type _ when targetType == typeof(QueueMessage) => ConvertToQueueMessage(modelBindingData),
            Type _ when targetType == typeof(BinaryData) => ConvertToBinaryData(modelBindingData),
            Type _ when targetType == typeof(JObject) => ConvertToJObject(modelBindingData),
            _ => null
        };

        private QueueMessage ConvertToQueueMessage(ModelBindingData modelBindingData)
        {
            JsonSerializerOptions options = new() { Converters = { new QueueMessageJsonConverter() } };
            return modelBindingData.Content.ToObjectFromJson<QueueMessage>(options);
        }

        private BinaryData ConvertToBinaryData(ModelBindingData modelBindingData)
        {
            return modelBindingData.Content;
        }

        private JObject ConvertToJObject(ModelBindingData modelBindingData)
        {
            return JObject.Parse(modelBindingData.Content.ToString());
        }
    }
}