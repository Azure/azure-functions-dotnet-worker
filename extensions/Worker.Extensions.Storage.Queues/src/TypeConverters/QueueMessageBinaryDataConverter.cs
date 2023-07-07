// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Storage.Queues;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="BinaryData" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(BinaryData))]
    internal sealed class QueueMessageBinaryDataConverter : QueueConverterBase<BinaryData>
    {
        public QueueMessageBinaryDataConverter() : base()
        {
        }

        protected override async ValueTask<BinaryData> ConvertCoreAsync(ModelBindingData data)
        {
            var messageText = await ExtractQueueMessageTextAsStringAsync(data);
            return new BinaryData(messageText);
        }

        private async Task<string> ExtractQueueMessageTextAsStringAsync(ModelBindingData modelBindingData)
        {
            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new InvalidContentTypeException(Constants.JsonContentType);
            }

            using var contentStream = modelBindingData.Content.ToStream();
            var contentElement = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream).ConfigureAwait(false);

            return contentElement.GetProperty(Constants.QueueMessageText).ToString()
                                ?? throw new InvalidOperationException($"The '{Constants.QueueMessageText}' property is missing or null.");
        }
    }
}
