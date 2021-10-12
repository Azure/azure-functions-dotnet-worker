// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Grpc.Features
{
    internal class GrpcFunctionBindingsFeature : IFunctionBindingsFeature
    {
        private readonly FunctionContext _context;
        private readonly InvocationRequest _invocationRequest;
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;

        private IReadOnlyDictionary<string, object?>? _triggerMetadata;
        private IReadOnlyDictionary<string, object?>? _inputData;
        private IDictionary<string, object?>? _outputData;
        private OutputBindingsInfo? _outputBindings;

        public GrpcFunctionBindingsFeature(FunctionContext context, InvocationRequest invocationRequest, IOutputBindingsInfoProvider outputBindingsInfoProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _invocationRequest = invocationRequest ?? throw new ArgumentNullException(nameof(invocationRequest));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
        }

        public IReadOnlyDictionary<string, object?> TriggerMetadata
        {
            get
            {
                if (_triggerMetadata is null)
                {
                    _triggerMetadata = ToReadOnlyDictionary(_invocationRequest.TriggerMetadata, _context);
                }

                return _triggerMetadata;
            }
        }

        public IReadOnlyDictionary<string, object?> InputData
        {
            get
            {
                if (_inputData is null)
                {
                    _inputData = ToReadOnlyCollection(_invocationRequest.InputData, _context);
                }

                return _inputData;
            }
        }

        public IDictionary<string, object?> OutputBindingData
        {
            get
            {
                if (_outputData is null)
                {
                    _outputData = new Dictionary<string, object?>();
                }

                return _outputData;
            }
        }

        public OutputBindingsInfo OutputBindingsInfo
        {
            get
            {
                if (_outputBindings is null)
                {
                    _outputBindings = _outputBindingsInfoProvider.GetBindingsInfo(_context.FunctionDefinition);
                }

                return _outputBindings;
            }
        }

        public object? InvocationResult { get; set; }


        public void SetOutputBinding(string name, object value)
        {
            throw new NotImplementedException();
        }

        private static IReadOnlyDictionary<string, object?> ToReadOnlyDictionary(IDictionary<string, TypedData>? map, FunctionContext context)
        {
            if (map is null)
            {
                return ImmutableDictionary<string, object?>.Empty;
            }

            return new ReadOnlyDictionary<string, object?>(Enumerable.ToDictionary(map, kvp => kvp.Key, kvp => ConvertTypedData(kvp.Value, context), StringComparer.OrdinalIgnoreCase));
        }

        private static IReadOnlyDictionary<string, object?> ToReadOnlyCollection(IEnumerable<ParameterBinding> map, FunctionContext context)
        {
            if (map is null)
            {
                return ImmutableDictionary<string, object?>.Empty;
            }

            return new ReadOnlyDictionary<string, object?>(Enumerable.ToDictionary(map, p => p.Name, p => ConvertTypedData(p.Data, context), StringComparer.OrdinalIgnoreCase));
        }

        private static object? ConvertTypedData(TypedData typedData, FunctionContext context)
        {
            return typedData.DataCase switch
            {
                TypedData.DataOneofCase.None => null,
                TypedData.DataOneofCase.Http => new GrpcHttpRequestData(typedData.Http, context),
                TypedData.DataOneofCase.String => typedData.String,
                // This is guaranteed to be Json here -- we can use that.
                TypedData.DataOneofCase.Json => typedData.Json,
                TypedData.DataOneofCase.Bytes => typedData.Bytes.Memory,
                TypedData.DataOneofCase.CollectionBytes => typedData.CollectionBytes.Bytes.Select(element => {
                    return element.Memory;
                }),
                TypedData.DataOneofCase.CollectionString => typedData.CollectionString.String,
                TypedData.DataOneofCase.CollectionDouble => typedData.CollectionDouble.Double,
                TypedData.DataOneofCase.CollectionSint64 => typedData.CollectionSint64.Sint64,
                _ => throw new NotSupportedException($"{typedData.DataCase} is not supported."),
            };
        }
    }
}
