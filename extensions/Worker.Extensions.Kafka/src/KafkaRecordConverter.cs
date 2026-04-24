// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Kafka.Proto;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="KafkaRecord"/> or <see cref="KafkaRecord[]"/> type parameters.
    /// Deserializes Protobuf-encoded ModelBindingData from the host-side Kafka extension.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(KafkaRecord))]
    [SupportedTargetType(typeof(KafkaRecord[]))]
    internal class KafkaRecordConverter : IInputConverter
    {
        internal const string ExpectedBindingSource = "AzureKafkaRecord";
        internal const string ExpectedContentType = "application/x-protobuf";

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                ConversionResult result = context?.Source switch
                {
                    ModelBindingData binding => ConversionResult.Success(ConvertToKafkaRecord(binding)),
                    CollectionModelBindingData collection => ConversionResult.Success(
                        collection.ModelBindingData.Select(ConvertToKafkaRecord).ToArray()),
                    _ => ConversionResult.Unhandled(),
                };

                return new ValueTask<ConversionResult>(result);
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }

        private static KafkaRecord ConvertToKafkaRecord(ModelBindingData binding)
        {
            if (binding is null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (binding.Source is not ExpectedBindingSource)
            {
                throw new InvalidBindingSourceException(binding.Source, ExpectedBindingSource);
            }

            if (binding.ContentType is not ExpectedContentType)
            {
                throw new InvalidContentTypeException(binding.ContentType, ExpectedContentType);
            }

            var proto = KafkaRecordProto.Parser.ParseFrom(binding.Content.ToMemory().Span);

            return new KafkaRecord
            {
                Topic = proto.Topic,
                Partition = proto.Partition,
                Offset = proto.Offset,
                Key = proto.HasKey ? proto.Key.ToByteArray() : null,
                Value = proto.HasValue ? proto.Value.ToByteArray() : null,
                LeaderEpoch = proto.HasLeaderEpoch ? proto.LeaderEpoch : (int?)null,
                Timestamp = proto.Timestamp != null
                    ? new KafkaTimestamp
                    {
                        UnixTimestampMs = proto.Timestamp.UnixTimestampMs,
                        Type = System.Enum.IsDefined(typeof(KafkaTimestampType), proto.Timestamp.Type)
                            ? (KafkaTimestampType)proto.Timestamp.Type
                            : KafkaTimestampType.NotAvailable,
                    }
                    : null,
                Headers = proto.Headers.Select(h => new KafkaHeader
                {
                    Key = h.Key,
                    Value = h.HasValue ? h.Value.ToByteArray() : null,
                }).ToArray(),
            };
        }
    }
}
