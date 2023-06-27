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

        protected override ValueTask<BinaryData> ConvertCoreAsync(ModelBindingData data)
        {
            return new ValueTask<BinaryData>(ExtractQueueMessageContent(data));
        }

        private BinaryData ExtractQueueMessageContent(ModelBindingData modelBindingData)
        {
            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new InvalidContentTypeException(modelBindingData.ContentType, Constants.JsonContentType);
            }

            var content = modelBindingData.Content.ToObjectFromJson<JsonElement>();
            var messageText = content.GetProperty(Constants.QueueMessageText).ToString()
                    ?? throw new InvalidOperationException($"The '{Constants.QueueMessageText}' property is missing or null.");

            return new BinaryData(messageText);
        }
    }
}
