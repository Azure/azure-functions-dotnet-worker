// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportsJsonDeserialization]
    [SupportedConverterType(typeof(BinaryData))]
    internal sealed class QueueMessageBinaryDataConverter : QueueConverterBase<BinaryData>
    {
        public QueueMessageBinaryDataConverter(ILogger<QueueMessageBinaryDataConverter> logger) : base(logger)
        {
        }

        public override async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!CanConvert(context))
            {
                return ConversionResult.Unhandled();
            }

            try
            {
                var modelBindingData = (ModelBindingData)context.Source!;
                var messageText = await ExtractQueueMessageTextAsStringAsync(modelBindingData);
                var result = new BinaryData(messageText);

                return ConversionResult.Success(result);
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses Json.NET serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the queue payload to be valid json. The JSON parser failed: {0}",
                    ex.Message);

                return ConversionResult.Failed(new InvalidOperationException(msg));
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }
    }
}