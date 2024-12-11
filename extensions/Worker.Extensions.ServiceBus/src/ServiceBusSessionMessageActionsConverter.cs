// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.ServiceBus.Grpc;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="ServiceBusSessionMessageActions" /> or <see cref="ServiceBusSessionMessageActions{}" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(ServiceBusSessionMessageActions))]
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
                var sessionId = ParseSessionIdFromBindingData(context);

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

                var sessionActionResult = new ServiceBusSessionMessageActions(_settlement, sessionId, sessionLockedUntil.GetDateTimeOffset());
                var result = ConversionResult.Success(sessionActionResult);
                return new ValueTask<ConversionResult>(result);
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }

        private string ParseSessionIdFromBindingData(ConverterContext context)
        {
            // Try to resolve sessionId directly
            var bindingData = context.FunctionContext.BindingContext.BindingData;
            bindingData.TryGetValue("SessionId", out object? sessionId);

            // If sessionId is not found and sessionIdRepeatedFieldArray has a value (isBatched = true), we can just parse the first sessionId from the array, as all the values are guaranteed to be the same.
            // This is because there can be multiple messages but each message would belong to the same session.
            // Note if web jobs extensions ever adds support for multiple sessions in a single batch, this logic will need to be updated.
            if (sessionId == null && bindingData.TryGetValue("SessionIdArray", out object? sessionIdArray))
            {
                var sessionIdRepeatedArray = sessionIdArray as IList<string>;
                if (sessionIdRepeatedArray is not null && sessionIdRepeatedArray.Count > 0)
                {
                    sessionId = sessionIdRepeatedArray[0]; // Use the first sessionId in the array
                }
            }

            if (sessionId == null)
            {
                throw new InvalidOperationException(
                    $"Expecting SessionId or SessionIdArray within binding data and value was not present. Sessions must be enabled when binding to {nameof(ServiceBusSessionMessageActions)}.");
            }

            return sessionId.ToString();
        }
    }
}
