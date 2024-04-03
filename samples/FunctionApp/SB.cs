using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class SB
    {
        private readonly ILogger<SB> _logger;

        public SB(ILogger<SB> logger)
        {
            _logger = logger;
        }

        [Function(nameof(SB))]
        [ServiceBusOutput("myqueue2", Connection = "sbcon")]
        public string ServiceBusReceivedMessageFunction([ServiceBusTrigger("myqueue1", Connection = "sbcon")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            var outputMessage = $"Output message created at {DateTime.Now}";
            return outputMessage;
        }
    }
}
