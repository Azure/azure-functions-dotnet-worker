// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace MiddlewareBinding
{
    /// <summary>
    /// Reads the Service Bus trigger binding from inside the middleware pipeline,
    /// before the function itself runs.
    /// </summary>
    internal class ServiceBusMessageMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<ServiceBusMessageMiddleware> _logger;

        public ServiceBusMessageMiddleware(ILogger<ServiceBusMessageMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // Find the trigger binding of the function that is about to run.
            var triggerBinding = context.FunctionDefinition.InputBindings.Values
                .FirstOrDefault(b => b.Type == "serviceBusTrigger");

            if (triggerBinding is not null)
            {
                // BindInputAsync converts the binding data to the requested type.
                // The result is cached on the context, so the function receives the same value.
                var bindingData = await context.BindInputAsync<ServiceBusReceivedMessage>(triggerBinding);

                if (bindingData?.Value is { } message)
                {
                    _logger.LogInformation("Middleware received Service Bus message {id}", message.MessageId);
                }
            }

            await next(context);
        }
    }
}
