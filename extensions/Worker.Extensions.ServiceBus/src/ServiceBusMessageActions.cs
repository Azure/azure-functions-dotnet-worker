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

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            await _settlement.CompleteAsync(new() { Locktoken = message.LockToken}, cancellationToken: cancellationToken);
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
                PropertiesToModify = { TransformProperties(propertiesToModify) }
            };
            if (deadLetterReason != null)
            {
                request.DeadletterReason = deadLetterReason;
            }

            if (deadLetterErrorDescription != null)
            {
                request.DeadletterErrorDescription = deadLetterErrorDescription;
            }
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
                switch (kvp.Value)
                {
                    case string stringValue:
                        converted.Add(kvp.Key, new SettlementProperties() { StringValue = stringValue });
                        break;
                    case bool boolValue:
                        converted.Add(kvp.Key, new SettlementProperties() { BoolValue = boolValue });
                        break;
                    case byte byteValue:
                        // proto does not support single byte, so use int
                        converted.Add(kvp.Key, new SettlementProperties() { IntValue = byteValue });
                        break;
                    case sbyte sbyteValue:
                        // proto does not support single byte, so use int
                        converted.Add(kvp.Key, new SettlementProperties() { IntValue = sbyteValue });
                        break;
                    case short shortValue:
                        // proto does not support short, so use int
                        converted.Add(kvp.Key, new SettlementProperties() { IntValue = shortValue });
                        break;
                    case ushort ushortValue:
                        // proto does not support short, so use int
                        converted.Add(kvp.Key, new SettlementProperties() { IntValue = ushortValue });
                        break;
                    case int intValue:
                        converted.Add(kvp.Key, new SettlementProperties() { IntValue = intValue });
                        break;
                    case uint uintValue:
                        converted.Add(kvp.Key, new SettlementProperties() { UintValue = uintValue });
                        break;
                    case long longValue:
                        converted.Add(kvp.Key, new SettlementProperties() { LongValue = longValue });
                        break;
                    case ulong ulongValue:
                        // proto does not support ulong, so use double
                        converted.Add(kvp.Key, new SettlementProperties() { DoubleValue = ulongValue });
                        break;
                    case double doubleValue:
                        converted.Add(kvp.Key, new SettlementProperties() { DoubleValue = doubleValue });
                        break;
                    case decimal decimalValue:
                        // proto does not support decimal, so use double
                        converted.Add(kvp.Key, new SettlementProperties() { DoubleValue = Decimal.ToDouble(decimalValue) });
                        break;
                    case float floatValue:
                        converted.Add(kvp.Key, new SettlementProperties() { FloatValue = floatValue });
                        break;
                    case char charValue:
                        converted.Add(kvp.Key, new SettlementProperties() { StringValue = charValue.ToString() });
                        break;
                    case Guid guidValue:
                        converted.Add(kvp.Key, new SettlementProperties() { StringValue = guidValue.ToString() });
                        break;
                    case DateTimeOffset dateTimeOffsetValue:
                        // proto does not support DateTimeOffset, so use Timestamp from google.protobuf
                        converted.Add(kvp.Key, new SettlementProperties() { TimestampValue = Timestamp.FromDateTimeOffset(dateTimeOffsetValue) });
                        break;
                    case DateTime dateTimeValue:
                        // proto does not support DateTime, so use Timestamp from google.protobuf
                        converted.Add(kvp.Key, new SettlementProperties() { TimestampValue = Timestamp.FromDateTimeOffset(dateTimeValue) });
                        break;
                    case Uri uriValue:
                        // proto does not support Uri, so use string
                        converted.Add(kvp.Key, new SettlementProperties() { StringValue = uriValue.ToString() });
                        break;
                    case TimeSpan timeSpanValue:
                        // proto does not support TimeSpan, so use string
                        converted.Add(kvp.Key, new SettlementProperties() { StringValue = timeSpanValue.ToString() });
                        break;
                }
            }

            return converted;
        }
    }
}