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

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportedConverterType(typeof(QueueMessage))]
    internal sealed class QueueMessageConverter : QueueConverterBase<QueueMessage>
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public QueueMessageConverter() : base()
        {
            _jsonOptions = new() { Converters = { new QueueMessageJsonConverter() } };
        }

        protected override ValueTask<QueueMessage> ConvertCoreAsync(ModelBindingData data)
        {
            return new ValueTask<QueueMessage>(Task.FromResult(ExtractQueueMessage(data)));
        }

        private QueueMessage ExtractQueueMessage(ModelBindingData modelBindingData)
        {
            if (modelBindingData.ContentType is not Constants.JsonContentType)
            {
                throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.");
            }

            return modelBindingData.Content.ToObjectFromJson<QueueMessage>(_jsonOptions);
        }
    }
}
