// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    internal abstract class EventGridConverterBase<T> : IInputConverter
    {
        private readonly ILogger<EventGridConverterBase<T>> _logger;

        public EventGridConverterBase(ILogger<EventGridConverterBase<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected bool CanConvert(ConverterContext context)
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

            if (bindingData.Source is not Constants.EventGridExtensionName)
            {
                return false;
            }

            return true;
        }

        public abstract ValueTask<ConversionResult> ConvertAsync(ConverterContext context);

        protected Dictionary<string, string> GetBindingDataContent(ModelBindingData? bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, string>(bindingData?.Content?.ToObjectFromJson<Dictionary<string, string>>(), StringComparer.OrdinalIgnoreCase),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.")
            };
        }
    }
}
