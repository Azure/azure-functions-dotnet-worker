// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.ServiceBus.Grpc;
using System.Text.Json;

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
                var foundSessionId = context.FunctionContext.BindingContext.BindingData.TryGetValue("SessionId", out object? sessionId);
                if (!foundSessionId)
                {
                    throw new InvalidOperationException("Expecting SessionId within binding data and value was not present.");
                }

                // Get the sessionLockedUntil property from the SessionActions binding data
                var foundSessionActions = context.FunctionContext.BindingContext.BindingData.TryGetValue("SessionActions", out object? sessionActions);
                if (!foundSessionActions)
                {
                    throw new InvalidOperationException("Expecting SessionActions within binding data and value was not present.");
                }

                JsonDocument jsonDocument = JsonDocument.Parse(sessionActions!.ToString());
                var foundSessionLockedUntil = jsonDocument.RootElement.TryGetProperty("SessionLockedUntil", out JsonElement sessionLockedUntil);
                if (!foundSessionLockedUntil)
                {
                    throw new InvalidOperationException("Expecting SessionLockedUntil within binding data of session actions and value was not present.");
                }

                var sessionActionResult = new ServiceBusSessionMessageActions(_settlement, sessionId!.ToString(), sessionLockedUntil.GetDateTimeOffset());
                var result = ConversionResult.Success(sessionActionResult);
                return new ValueTask<ConversionResult>(result);
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }
    }
}
