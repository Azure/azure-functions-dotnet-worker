// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="ServiceBusMessageActions" /> type parameter.
    /// </summary>
    internal class ServiceBusMessageActionsConverter : IInputConverter
    {
        private readonly Settlement.SettlementClient _settlement;

        public ServiceBusMessageActionsConverter(Settlement.SettlementClient settlement)
        {
            _settlement = settlement;
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(new ServiceBusMessageActions(_settlement)));
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }
    }
}