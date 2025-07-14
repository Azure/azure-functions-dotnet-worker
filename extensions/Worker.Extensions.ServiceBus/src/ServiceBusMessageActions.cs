// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.ServiceBus.Grpc;
using Type = System.Type;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(ServiceBusMessageActionsConverter))]
    public class ServiceBusMessageActions
    {
        private readonly Settlement.SettlementClient _settlement;

        /// <summary>The size, in bytes, to use as a buffer for stream operations.</summary>
        private const int StreamBufferSizeInBytes = 512;

        /// <summary>The set of mappings from CLR types to AMQP types for property values.</summary>
        private static readonly IReadOnlyDictionary<Type, AmqpType> AmqpPropertyTypeMap = new Dictionary<Type, AmqpType>
        {
            { typeof(byte), AmqpType.Byte },
            { typeof(sbyte), AmqpType.SByte },
            { typeof(char), AmqpType.Char },
            { typeof(short), AmqpType.Int16 },
            { typeof(ushort), AmqpType.UInt16 },
            { typeof(int), AmqpType.Int32 },
            { typeof(uint), AmqpType.UInt32 },
            { typeof(long), AmqpType.Int64 },
            { typeof(ulong), AmqpType.UInt64 },
            { typeof(float), AmqpType.Single },
            { typeof(double), AmqpType.Double },
            { typeof(decimal), AmqpType.Decimal },
            { typeof(bool), AmqpType.Boolean },
            { typeof(Guid), AmqpType.Guid },
            { typeof(string), AmqpType.String },
            { typeof(Uri), AmqpType.Uri },
            { typeof(DateTime), AmqpType.DateTime },
            { typeof(DateTimeOffset), AmqpType.DateTimeOffset },
            { typeof(TimeSpan), AmqpType.TimeSpan },
        };

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
            _settlement = null!; // not expected to be used during mocking.
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _settlement.CompleteAsync(new() { Locktoken = message.LockToken }, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.AbandonMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, CancellationToken)"/>
        public virtual async Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? propertiesToModify = default,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = new AbandonRequest()
            {
                Locktoken = message.LockToken,
            };
            if (propertiesToModify != null)
            {
                request.PropertiesToModify = ConvertToByteString(propertiesToModify);
            }
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
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = new DeadletterRequest()
            {
                Locktoken = message.LockToken,
                DeadletterReason = deadLetterReason,
                DeadletterErrorDescription = deadLetterErrorDescription
            };
            if (propertiesToModify != null)
            {
                request.PropertiesToModify = ConvertToByteString(propertiesToModify);
            }
            await _settlement.DeadletterAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.DeferMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, CancellationToken)"/>
        public virtual async Task DeferMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? propertiesToModify = default,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = new DeferRequest()
            {
                Locktoken = message.LockToken,
            };
            if (propertiesToModify != null)
            {
                request.PropertiesToModify = ConvertToByteString(propertiesToModify);
            }
            await _settlement.DeferAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.RenewMessageLockAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task RenewMessageLockAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = new RenewMessageLockRequest()
            {
                Locktoken = message.LockToken,
            };

            await _settlement.RenewMessageLockAsync(request, cancellationToken: cancellationToken);
        }

        internal static ByteString ConvertToByteString(IDictionary<string, object> propertiesToModify)
        {
            var map = new AmqpMap();
            foreach (KeyValuePair<string, object> kvp in propertiesToModify)
            {
                if (TryCreateAmqpPropertyValueFromNetProperty(kvp.Value, out var amqpValue))
                {
                    map[new MapKey(kvp.Key)] = amqpValue;
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "The key `{0}` has a value of type `{1}` which is not supported for AMQP transport." +
                            "The list of supported types can be found here: https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.applicationproperties?view=azure-dotnet#remarks",
                            kvp.Key,
                            kvp.Value?.GetType().Name));
                }
            }

            using ByteBuffer buffer = new ByteBuffer(256, true);
            AmqpCodec.EncodeMap(map, buffer);
            return ByteString.CopyFrom(buffer.Buffer, 0, buffer.Length);
        }

        /// <summary>
        ///   Attempts to create an AMQP property value for a given event property.
        /// </summary>
        ///
        /// <param name="propertyValue">The value of the event property to create an AMQP property value for.</param>
        /// <param name="amqpPropertyValue">The AMQP property value that was created.</param>
        /// <param name="allowBodyTypes"><c>true</c> to allow an AMQP map to be translated to additional types supported only by a message body; otherwise, <c>false</c>.</param>
        ///
        /// <returns><c>true</c> if an AMQP property value was able to be created; otherwise, <c>false</c>.</returns>
        ///
        private static bool TryCreateAmqpPropertyValueFromNetProperty(
            object? propertyValue,
            out object? amqpPropertyValue,
            bool allowBodyTypes = false)
        {
            amqpPropertyValue = null;

            if (propertyValue == null)
            {
                return true;
            }

            switch (GetTypeIdentifier(propertyValue))
            {
                case AmqpType.Byte:
                case AmqpType.SByte:
                case AmqpType.Int16:
                case AmqpType.Int32:
                case AmqpType.Int64:
                case AmqpType.UInt16:
                case AmqpType.UInt32:
                case AmqpType.UInt64:
                case AmqpType.Single:
                case AmqpType.Double:
                case AmqpType.Boolean:
                case AmqpType.Decimal:
                case AmqpType.Char:
                case AmqpType.Guid:
                case AmqpType.DateTime:
                case AmqpType.String:
                    amqpPropertyValue = propertyValue;
                    break;

                case AmqpType.Stream:
                case AmqpType.Unknown when propertyValue is Stream:
                    amqpPropertyValue = ReadStreamToArraySegment((Stream)propertyValue);
                    break;

                case AmqpType.Uri:
                    amqpPropertyValue = new DescribedType((AmqpSymbol)AmqpMessageConstants.Uri, ((Uri)propertyValue).AbsoluteUri);
                    break;

                case AmqpType.DateTimeOffset:
                    amqpPropertyValue = new DescribedType((AmqpSymbol)AmqpMessageConstants.DateTimeOffset, ((DateTimeOffset)propertyValue).UtcTicks);
                    break;

                case AmqpType.TimeSpan:
                    amqpPropertyValue = new DescribedType((AmqpSymbol)AmqpMessageConstants.TimeSpan, ((TimeSpan)propertyValue).Ticks);
                    break;

                case AmqpType.Unknown when allowBodyTypes && propertyValue is byte[] byteArray:
                    amqpPropertyValue = new ArraySegment<byte>(byteArray);
                    break;

                case AmqpType.Unknown when allowBodyTypes && propertyValue is IDictionary dict:
                    amqpPropertyValue = new AmqpMap(dict);
                    break;

                case AmqpType.Unknown when allowBodyTypes && propertyValue is IList:
                    amqpPropertyValue = propertyValue;
                    break;

                case AmqpType.Unknown:
                    var exception = new SerializationException(string.Format(CultureInfo.CurrentCulture, "Serialization failed due to an unsupported type, {0}.", propertyValue.GetType().FullName));
                    throw exception;
            }

            return (amqpPropertyValue != null);
        }

        /// <summary>
        ///   Converts a stream to an <see cref="ArraySegment{T}" /> representation.
        /// </summary>
        ///
        /// <param name="stream">The stream to read and capture in memory.</param>
        ///
        /// <returns>The <see cref="ArraySegment{T}" /> containing the stream data.</returns>
        ///
        private static ArraySegment<byte> ReadStreamToArraySegment(Stream stream)
        {
            switch (stream)
            {
                case { Length: < 1 }:
                    return default;

                case BufferListStream bufferListStream:
                    return bufferListStream.ReadBytes((int)stream.Length);

                case MemoryStream memStreamSource:
                {
                    using var memStreamCopy = new MemoryStream((int)(memStreamSource.Length - memStreamSource.Position));
                    memStreamSource.CopyTo(memStreamCopy, StreamBufferSizeInBytes);
                    if (!memStreamCopy.TryGetBuffer(out ArraySegment<byte> segment))
                    {
                        segment = new ArraySegment<byte>(memStreamCopy.ToArray());
                    }
                    return segment;
                }

                default:
                {
                    using var memStreamCopy = new MemoryStream(StreamBufferSizeInBytes);
                    stream.CopyTo(memStreamCopy, StreamBufferSizeInBytes);
                    if (!memStreamCopy.TryGetBuffer(out ArraySegment<byte> segment))
                    {
                        segment = new ArraySegment<byte>(memStreamCopy.ToArray());
                    }
                    return segment;
                }
            }
        }

        /// <summary>
        ///   Represents the supported AMQP property types.
        /// </summary>
        ///
        /// <remarks>
        ///   WARNING:
        ///     These values are synchronized between Azure services and the client
        ///     library.  You must consult with the Event Hubs/Service Bus service team before making
        ///     changes, including adding a new member.
        ///
        ///     When adding a new member, remember to always do so before the Unknown
        ///     member.
        /// </remarks>
        ///
        private enum AmqpType
        {
            Null,
            Byte,
            SByte,
            Char,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,
            Single,
            Double,
            Decimal,
            Boolean,
            Guid,
            String,
            Uri,
            DateTime,
            DateTimeOffset,
            TimeSpan,
            Stream,
            Unknown
        }

        /// <summary>
        ///   Gets the AMQP property type identifier for a given
        ///   value.
        /// </summary>
        ///
        /// <param name="value">The value to determine the type identifier for.</param>
        ///
        /// <returns>The <see cref="Type"/> that was identified for the given <paramref name="value"/>.</returns>
        ///
        private static AmqpType GetTypeIdentifier(object? value) => ToAmqpPropertyType(value?.GetType());

        /// <summary>
        ///   Translates the given <see cref="Type" /> to the corresponding
        ///   <see cref="AmqpType" />.
        /// </summary>
        ///
        /// <param name="type">The type to convert to an AMQP type.</param>
        ///
        /// <returns>The AMQP property type that best matches the specified <paramref name="type"/>.</returns>
        ///
        private static AmqpType ToAmqpPropertyType(Type? type)
        {
            if (type == null)
            {
                return AmqpType.Null;
            }

            if (AmqpPropertyTypeMap.TryGetValue(type, out AmqpType amqpType))
            {
                return amqpType;
            }

            return AmqpType.Unknown;
        }

        internal static class AmqpMessageConstants
        {
            public const string Vendor = "com.microsoft";
            public const string TimeSpan = Vendor + ":timespan";
            public const string Uri = Vendor + ":uri";
            public const string DateTimeOffset = Vendor + ":datetime-offset";
        }
    }
}
