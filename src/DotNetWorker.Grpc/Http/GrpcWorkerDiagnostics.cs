using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc
{

    internal class GrpcWorkerDiagnostics : IWorkerDiagnostics
    {
        private readonly ChannelWriter<StreamingMessage> _outputChannel;

        internal static readonly JsonSerializerOptions SerializerOptions;

        static GrpcWorkerDiagnostics()
        {
            SerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            SerializerOptions.Converters.Add(new TypeNameConverter());
        }

        public GrpcWorkerDiagnostics(GrpcHostChannel hostChannel)
        {
            if (hostChannel == null)
            {
                throw new ArgumentNullException(nameof(hostChannel));
            }

            if (hostChannel.Channel == null)
            {
                throw new InvalidOperationException($"{nameof(Channel)} cannot be null.");
            }

            _outputChannel = hostChannel.Channel.Writer ?? throw new InvalidOperationException($"Writer cannot be null.");
        }

        public void OnApplicationCreated(WorkerInformation workerInfo)
        {
            var message = new StreamingMessage
            {
                RpcLog = new RpcLog
                {
                    EventId = nameof(OnApplicationCreated),
                    Level = RpcLog.Types.Level.Debug,
                    LogCategory = RpcLog.Types.RpcLogCategory.System,
                    Message = JsonSerializer.Serialize(workerInfo, SerializerOptions)
                }
            };

            _outputChannel.TryWrite(message);
        }

        public void OnFunctionLoaded(FunctionDefinition definition)
        {
            var message = new StreamingMessage
            {
                RpcLog = new RpcLog
                {
                    EventId = nameof(OnFunctionLoaded),
                    Level = RpcLog.Types.Level.Debug,
                    LogCategory = RpcLog.Types.RpcLogCategory.System,
                    Message = JsonSerializer.Serialize(definition, SerializerOptions)
                }
            };
        }

        private class TypeNameConverter : JsonConverter<Type>
        {
            public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // We'll never deserialize.
                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.FullName);
            }
        }
    }
}
