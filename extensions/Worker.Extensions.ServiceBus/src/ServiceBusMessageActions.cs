using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Grpc.Core;
using Grpc.Core.Logging;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(ServiceBusMessageActionsConverter))]
    public class ServiceBusMessageActions
    {
        private readonly Settlement.SettlementClient _settlement;

        // internal ServiceBusMessageActions(ISettlement settlement)
        // {
        //     _settlement = settlement;
        // }

        // public ServiceBusMessageActions()
        // {
        //     _settlement = new Settlement.SettlementClient();
        // }

        internal ServiceBusMessageActions(Settlement.SettlementClient settlement)
        {
            _settlement = settlement;
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            // _logger.LogInformation("Completing message with lock token {LockToken}", lockToken);
            CompleteReply reply = await _settlement.CompleteAsync(new() { Locktoken = message.LockToken}, cancellationToken: cancellationToken);
            // return reply;
            // await _settlement.CompleteAsync(message.LockToken, cancellationToken);
        }
    }
}