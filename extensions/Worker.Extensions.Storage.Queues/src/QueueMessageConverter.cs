// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportsJsonDeserialization]
    [SupportedConverterType(typeof(QueueMessage))]
    [SupportedConverterType(typeof(BinaryData))]
    [SupportedConverterType(typeof(JsonElement))]
    internal class QueueMessageConverter : IInputConverter
    {
        private readonly ObjectSerializer _serializer;
        private readonly ILogger<QueueMessageConverter> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public QueueMessageConverter(IOptions<WorkerOptions> workerOptions, ILogger<QueueMessageConverter> logger)
        {
            _serializer = workerOptions.Value.Serializer ?? throw new InvalidOperationException(nameof(workerOptions.Value.Serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new() { Converters = { new QueueMessageJsonConverter() } };
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

            try
            {
                QueueMessage queueMessage = ExtractQueueMessageFromBindingData(modelBindingData);

                return context.TargetType switch
                {
                    Type _ when context.TargetType == typeof(QueueMessage) => ConversionResult.Success(queueMessage),
                    Type _ when context.TargetType == typeof(BinaryData) => ConversionResult.Success(ConvertMessageContentToBinaryData(queueMessage)),
                    Type _ when context.TargetType == typeof(JsonElement) => ConversionResult.Success(ConvertMessageContentToJsonElement(queueMessage, context.TargetType)),
                    _ => ConversionResult.Unhandled(),
                };
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses Json.NET serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the queue payload to be valid json. The JSON parser failed: {0}",
                    ex.Message);

                return ConversionResult.Failed(new InvalidOperationException(msg, ex));
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private bool IsQueueExtension(ModelBindingData bindingData)
        {
            bool isQueueExtension = bindingData.Source is Constants.QueueExtensionName;

            if (!isQueueExtension)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(QueueMessageConverter));
            }

            return isQueueExtension;
        }

        private QueueMessage ExtractQueueMessageFromBindingData(ModelBindingData modelBindingData)
        {
            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.");
            }

            return modelBindingData.Content.ToObjectFromJson<QueueMessage>(_jsonOptions);
        }

        private BinaryData ConvertMessageContentToBinaryData(QueueMessage queueMessage)
        {
            return queueMessage.Body;
        }

        private async ValueTask<object?> ConvertMessageContentToJsonElement(QueueMessage queueMessage, Type targetType)
        {
            using var bodyStream = queueMessage.Body.ToStream();
            return await _serializer.DeserializeAsync(bodyStream, targetType, CancellationToken.None).ConfigureAwait(false);
        }
    }
}