using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Context
{
    internal class GrpcValueProvider : IValueProvider
    {
        private readonly IDictionary<string, ParameterBinding> _inputData;

        public GrpcValueProvider(IEnumerable<ParameterBinding> inputData)
        {
            _inputData = inputData.ToDictionary(k => k.Name);
        }

        public object? GetValue(string name)
        {
            if (!_inputData.TryGetValue(name, out ParameterBinding? binding))
            {
                return null;
            }

            var value = binding.Data;
            return value.DataCase switch
            {
                TypedData.DataOneofCase.None => null,
                TypedData.DataOneofCase.Http => new GrpcHttpRequestData(value.Http),
                TypedData.DataOneofCase.String => value.String,
                // This is guaranteed to be Json here -- we can use that.
                TypedData.DataOneofCase.Json => value.Json,
                _ => throw new NotSupportedException($"{value.DataCase} is not supported yet."),
            };
        }
    }
}
