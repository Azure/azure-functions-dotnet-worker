// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Context
{
    internal class GrpcValueProvider : IValueProvider
    {
        private readonly IDictionary<string, TypedData> _inputData;
        private readonly IDictionary<string, TypedData> _triggerMetadata;

        public GrpcValueProvider(IEnumerable<ParameterBinding> inputData, MapField<string, TypedData> triggerMetadata)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException(nameof(inputData));
            }

            if (triggerMetadata == null)
            {
                throw new ArgumentNullException(nameof(triggerMetadata));
            }

            _inputData = inputData.ToDictionary(kvp => kvp.Name, kvp => kvp.Data, StringComparer.OrdinalIgnoreCase);
            _triggerMetadata = triggerMetadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        public object? GetValue(string name, FunctionContext functionContext)
        {
            TypedData? value;

            if (_inputData.TryGetValue(name, out TypedData? inputValue))
            {
                value = inputValue;
            }
            else if (_triggerMetadata.TryGetValue(name, out TypedData? triggerValue))
            {
                value = triggerValue;
            }
            else
            {
                return null;
            }

            return value.DataCase switch
            {
                TypedData.DataOneofCase.None => null,
                TypedData.DataOneofCase.Http => new GrpcHttpRequestData(value.Http, functionContext),
                TypedData.DataOneofCase.String => value.String,
                // This is guaranteed to be Json here -- we can use that.
                TypedData.DataOneofCase.Json => value.Json,
                TypedData.DataOneofCase.Bytes => value.Bytes.Memory,
                TypedData.DataOneofCase.CollectionBytes => value.CollectionBytes.Bytes.Select(element => {
                    return element.Memory.ToArray();
                }),
                TypedData.DataOneofCase.CollectionString => value.CollectionString.String,
                TypedData.DataOneofCase.CollectionDouble => value.CollectionDouble.Double,
                TypedData.DataOneofCase.CollectionSint64 => value.CollectionSint64.Sint64,
                _ => throw new NotSupportedException($"{value.DataCase} is not supported yet."),
            };
        }
    }
}
