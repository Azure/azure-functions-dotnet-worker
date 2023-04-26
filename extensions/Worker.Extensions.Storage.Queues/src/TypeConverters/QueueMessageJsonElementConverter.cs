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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportsJsonDeserialization]
    [SupportedConverterType(typeof(JsonElement))]
    internal sealed class QueueMessageJsonElementConverter : QueueConverterBase<JsonElement>
    {
        private readonly ObjectSerializer _serializer;

        public QueueMessageJsonElementConverter(IOptions<WorkerOptions> workerOptions, ILogger<QueueMessageJsonElementConverter> logger) : base(logger)
        {
            _serializer = workerOptions.Value.Serializer ?? throw new InvalidOperationException(nameof(workerOptions.Value.Serializer));
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
                QueueMessage queueMessage = ExtractQueueMessageFromBindingData(modelBindingData);

                using var bodyStream = queueMessage.Body.ToStream();
                var result = await _serializer.DeserializeAsync(bodyStream, context.TargetType, CancellationToken.None).ConfigureAwait(false);

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