// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
using Microsoft.Azure.ServiceBus.Grpc;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="ServiceBusSessionMessageActions" /> or <see cref="ServiceBusSessionMessageActions[]" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(ServiceBusSessionMessageActions))]
    [SupportedTargetType(typeof(ServiceBusSessionMessageActions[]))]
    internal class ServiceBusSessionMessageActionsConverter : IInputConverter
    {
        private readonly Settlement.SettlementClient _settlement;

        public ServiceBusSessionMessageActionsConverter(Settlement.SettlementClient settlement)
        {
            _settlement = settlement;
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                context.FunctionContext.BindingContext.BindingData.TryGetValue("SessionId", out object? sessionId);
                var result = ConversionResult.Success(new ServiceBusSessionMessageActions(_settlement, sessionId?.ToString()));
                return new ValueTask<ConversionResult>(result);
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }
    }
}
