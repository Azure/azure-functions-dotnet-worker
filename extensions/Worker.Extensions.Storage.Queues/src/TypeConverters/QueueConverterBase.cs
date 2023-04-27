// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    internal abstract class QueueConverterBase<T>  : IInputConverter
    {
        private readonly ILogger<QueueConverterBase<T>> _logger;

        public QueueConverterBase(ILogger<QueueConverterBase<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanConvert(ConverterContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.TargetType != typeof(T))
            {
                return false;
            }

            if (!(context.Source is ModelBindingData bindingData))
            {
                return false;
            }

            if (bindingData.Source is not Constants.QueueExtensionName)
            {
                return false;
            }

            return true;
        }

        public abstract ValueTask<ConversionResult> ConvertAsync(ConverterContext context);

        protected async Task<string> ExtractQueueMessageTextAsStringAsync(ModelBindingData modelBindingData)
        {
            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.");
            }

            using var contentStream = modelBindingData.Content.ToStream();
            var contentElement = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream).ConfigureAwait(false);

            return contentElement.GetProperty(Constants.QueueMessageText).ToString()
                                ?? throw new InvalidOperationException($"The '{Constants.QueueMessageText}' property is missing or null.");
        }
    }
}