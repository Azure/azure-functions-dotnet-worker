// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Storage.Queues;

namespace Microsoft.Azure.Functions.Worker
{
    internal abstract class QueueConverterBase<T>  : IInputConverter
    {

        public QueueConverterBase()
        {
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

            if (context.Source is not ModelBindingData bindingData)
            {
                return false;
            }

            if (bindingData.Source is not Constants.QueueExtensionName)
            {
                return false;
            }

            return true;
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!CanConvert(context))
            {
                ConversionResult.Unhandled();
            }

            try
            {
                var modelBindingData = (ModelBindingData)context.Source!;
                return ConversionResult.Success(ConvertCoreAsync(modelBindingData));
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses Json.NET serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the queue payload to be valid json.
                    The JSON parser failed: {0}",
                    ex.Message);

                return ConversionResult.Failed(new InvalidOperationException(msg));
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        protected abstract ValueTask<T> ConvertCoreAsync(ModelBindingData data);
    }
}
