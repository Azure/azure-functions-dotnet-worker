// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportsJsonDeserialization]
    [SupportedConverterType(typeof(QueueMessage))]
    internal sealed class QueueMessageConverter : QueueConverterBase<QueueMessage>
    {
        public QueueMessageConverter(ILogger<QueueMessageConverter> logger) : base(logger)
        {
        }

        public override ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!CanConvert(context))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            try
            {
                var modelBindingData = (ModelBindingData)context.Source!;
                QueueMessage queueMessage = ExtractQueueMessageFromBindingData(modelBindingData);
                return new ValueTask<ConversionResult>(ConversionResult.Success(queueMessage));
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses Json.NET serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the queue payload to be valid json. The JSON parser failed: {0}",
                    ex.Message);

                return new ValueTask<ConversionResult>(ConversionResult.Failed(new InvalidOperationException(msg)));
            }
            catch (Exception ex)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
            }
        }
    }
}