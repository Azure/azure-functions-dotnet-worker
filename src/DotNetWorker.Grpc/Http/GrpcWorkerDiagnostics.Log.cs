// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerDiagnostics
    {
        internal static class Log
        {
            internal static readonly JsonSerializerOptions SerializerOptions;

            static Log()
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                SerializerOptions.Converters.Add(new TypeNameConverter());
            }

            private static readonly Action<ILogger, string, Exception?> _applicationCreated =
               WorkerMessage.Define<string>(
                   LogLevel.Debug,
                   new EventId(1, nameof(ApplicationCreated)),
                   "{workerInfo}");

            private static readonly Action<ILogger, string, Exception?> _functionDefinitionCreated =
                WorkerMessage.Define<string>(
                    LogLevel.Debug,
                    new EventId(2, nameof(FunctionDefinitionCreated)),
                    "{functionDefinitionJson}");

            public static void ApplicationCreated(ILogger<GrpcWorkerDiagnostics> logger, WorkerInformation workerInfo)
            {
                var workerInfoJson = JsonSerializer.Serialize(workerInfo, SerializerOptions);
                _applicationCreated(logger, workerInfoJson, null);
            }

            public static void FunctionDefinitionCreated(ILogger<GrpcWorkerDiagnostics> logger, FunctionDefinition functionDefinition)
            {
                var definitionJson = JsonSerializer.Serialize(functionDefinition, SerializerOptions);
                _functionDefinitionCreated(logger, definitionJson, null);
            }

            private class TypeNameConverter : JsonConverter<Type>
            {
                public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    // We'll never deserialize.
                    throw new NotImplementedException();
                }

                public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
                {
                    writer.WriteStringValue(value.FullName);
                }
            }
        }
    }
}
