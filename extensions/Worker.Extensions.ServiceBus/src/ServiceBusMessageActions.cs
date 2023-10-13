using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.ServiceBus.Grpc;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(ServiceBusMessageActionsConverter))]
    public class ServiceBusMessageActions
    {
        private readonly Settlement.SettlementClient _settlement;

        internal ServiceBusMessageActions(Settlement.SettlementClient settlement)
        {
            _settlement = settlement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageActions"/> class for mocking use in testing.
        /// </summary>
        /// <remarks>
        /// This constructor exists only to support mocking. When used, class state is not fully initialized, and
        /// will not function correctly; virtual members are meant to be mocked.
        ///</remarks>
        protected ServiceBusMessageActions()
        {
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            await _settlement.CompleteAsync(new() { Locktoken = message.LockToken }, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? properties,
            CancellationToken cancellationToken = default)
        {
            var request = new AbandonRequest()
            {
                Locktoken = message.LockToken,
                PropertiesToModify = { TransformProperties(properties) }
            };
            await _settlement.AbandonAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.DeadLetterMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, string, string, CancellationToken)"/>
        public virtual async Task DeadLetterMessageAsync(
            ServiceBusReceivedMessage message,
            Dictionary<string, object>? propertiesToModify = default,
            string? deadLetterReason = default,
            string? deadLetterErrorDescription = default,
            CancellationToken cancellationToken = default)
        {
            var request = new DeadletterRequest()
            {
                Locktoken = message.LockToken,
                PropertiesToModify = { TransformProperties(propertiesToModify) },
                DeadletterReason = deadLetterReason,
                DeadletterErrorDescription = deadLetterErrorDescription
            };

            await _settlement.DeadletterAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.DeferMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, CancellationToken)"/>
        public virtual async Task DeferMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? propertiesToModify = default,
            CancellationToken cancellationToken = default)
        {
            var request = new DeferRequest()
            {
                Locktoken = message.LockToken,
                PropertiesToModify = { TransformProperties(propertiesToModify) }
            };
            await _settlement.DeferAsync(request, cancellationToken: cancellationToken);
        }

        private static Dictionary<string, SettlementProperties> TransformProperties(IDictionary<string, object>? properties)
        {
            var converted = new Dictionary<string, SettlementProperties>();
            if (properties == null)
            {
                return converted;
            }
            // support all types listed here - https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusmessage.applicationproperties?view=azure-dotnet
            foreach (var kvp in properties)
            {
                SettlementProperties settlementProperties = kvp.Value switch
                {
                    string stringValue => new SettlementProperties() { StringValue = stringValue },
                    bool boolValue => new SettlementProperties() { BoolValue = boolValue },
                    // proto does not support single byte, so use int
                    byte byteValue => new SettlementProperties() { IntValue = byteValue },
                    sbyte sbyteValue => new SettlementProperties() { IntValue = sbyteValue },
                    // proto does not support short, so use int
                    short shortValue => new SettlementProperties() { IntValue = shortValue },
                    ushort ushortValue => new SettlementProperties() { IntValue = ushortValue },
                    int intValue => new SettlementProperties() { IntValue = intValue },
                    uint uintValue => new SettlementProperties() { UintValue = uintValue },
                    long longValue => new SettlementProperties() { LongValue = longValue },
                    // proto does not support ulong, so use double
                    ulong ulongValue => new SettlementProperties() { DoubleValue = ulongValue },
                    double doubleValue => new SettlementProperties() { DoubleValue = doubleValue },
                    decimal decimalValue => new SettlementProperties() { DoubleValue = decimal.ToDouble(decimalValue) },
                    float floatValue => new SettlementProperties() { FloatValue = floatValue },
                    char charValue => new SettlementProperties() { StringValue = charValue.ToString() },
                    Guid guidValue => new SettlementProperties() { StringValue = guidValue.ToString() },
                    DateTimeOffset dateTimeOffsetValue => new SettlementProperties()
                        { TimestampValue = Timestamp.FromDateTimeOffset(dateTimeOffsetValue) },
                    // proto does not support DateTime, so use Timestamp from google.protobuf
                    DateTime dateTimeValue => new SettlementProperties() { TimestampValue = Timestamp.FromDateTimeOffset(dateTimeValue) },
                    // proto does not support Uri, so use string
                    Uri uriValue => new SettlementProperties() { StringValue = uriValue.ToString() },
                    // proto does not support TimeSpan, so use string
                    TimeSpan timeSpanValue => new SettlementProperties() { StringValue = timeSpanValue.ToString() },
                    _ => throw new NotSupportedException($"Unsupported property type {kvp.Value.GetType()}"),
                };
                converted.Add(kvp.Key, settlementProperties);
            }

            return converted;
        }
    }
}