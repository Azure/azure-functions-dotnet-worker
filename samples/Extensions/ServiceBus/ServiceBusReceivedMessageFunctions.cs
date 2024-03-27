// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Samples demonstrating binding to the <see cref="ServiceBusReceivedMessage"/> type.
    /// </summary>
    public class ServiceBusReceivedMessageFunctions
    {
        //<docsnippet_servicebusmessage_createlogger>
        private readonly ILogger<ServiceBusReceivedMessageFunctions> _logger;

        public ServiceBusReceivedMessageFunctions(ILogger<ServiceBusReceivedMessageFunctions> logger)
        {
            _logger = logger;
        }
        //</docsnippet_servicebusmessage_createlogger>
        /// <summary>
        /// This function demonstrates binding to a single <see cref="ServiceBusReceivedMessage"/>.
        /// </summary>
        //<docsnippet_servicebus_readmessage>
        [Function(nameof(ServiceBusReceivedMessageFunction))]
        [ServiceBusOutput("outputQueue", Connection = "ServiceBusConnection")]
        public string ServiceBusReceivedMessageFunction(
            [ServiceBusTrigger("queue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            var outputMessage = $"Output message created at {DateTime.Now}";
            return outputMessage;
        }
        //</docsnippet_servicebus_readmessage>
        /// <summary>
        /// This function demonstrates binding to an array of <see cref="ServiceBusReceivedMessage"/>.
        /// Note that when doing so, you must also set the <see cref="ServiceBusTriggerAttribute.IsBatched"/> property
        /// to <value>true</value>.
        /// </summary>
        //<docsnippet_servicebus_readbatch>
        [Function(nameof(ServiceBusReceivedMessageBatchFunction))]
        public void ServiceBusReceivedMessageBatchFunction(
            [ServiceBusTrigger("queue", Connection = "ServiceBusConnection", IsBatched = true)] ServiceBusReceivedMessage[] messages)
        {
            foreach (ServiceBusReceivedMessage message in messages)
            {
                _logger.LogInformation("Message ID: {id}", message.MessageId);
                _logger.LogInformation("Message Body: {body}", message.Body);
                _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
            }
        }
        //</docsnippet_servicebus_readbatch>
        /// <summary>
        /// This functions demonstrates that it is possible to bind to both the ServiceBusReceivedMessage and any of the supported binding contract
        /// properties at the same time. If attempting this, the ServiceBusReceivedMessage must be the first parameter. There is not
        /// much benefit to doing this as all of the binding contract properties are available as properties on the ServiceBusReceivedMessage.
        /// </summary>
        [Function(nameof(ServiceBusReceivedMessageWithStringProperties))]
        public void ServiceBusReceivedMessageWithStringProperties(
            [ServiceBusTrigger("queue", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message, string messageId, int deliveryCount)
        {
            // The MessageId property and the messageId parameter are the same.
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message ID: {id}", messageId);

            // Similarly the DeliveryCount property and the deliveryCount parameter are the same.
            _logger.LogInformation("Delivery Count: {count}", message.DeliveryCount);
            _logger.LogInformation("Delivery Count: {count}", deliveryCount);
        }
        //<docsnippet_servicebus_message_actions>
        [Function(nameof(ServiceBusMessageActionsFunction))]
        public async Task ServiceBusMessageActionsFunction(
            [ServiceBusTrigger("queue", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        //</docsnippet_servicebus_message_actions>
    }
}
