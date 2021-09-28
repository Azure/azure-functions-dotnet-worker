// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class JsonPocoConverter : IInputConverter
    {
        private readonly ObjectSerializer _serializer;

        public JsonPocoConverter(IOptions<WorkerOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Value.Serializer == null)
            {
                throw new InvalidOperationException(nameof(options.Value.Serializer));
            }

            _serializer = options.Value.Serializer;
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType == typeof(string))
            {
                return ConversionResult.Failed();
            }

            byte[]? bytes = null;

            if (context.Source is string sourceString)
            {
                bytes = Encoding.UTF8.GetBytes(sourceString);
            }
            else if (context.Source is ReadOnlyMemory<byte> sourceMemory)
            {
                bytes = sourceMemory.ToArray();
            }

            if (bytes == null)
            {
                return ConversionResult.Failed();
            }

            var deserializationResult = await TryDeserialize(bytes, context.TargetType);
            var bindingResult = new ConversionResult(deserializationResult.Success, deserializationResult.DeserializedObject);
            
            return bindingResult;
        }

        private async Task<(bool Success,object? DeserializedObject)> TryDeserialize(byte[] bytes, Type type)
        {
            try
            {
                await using (var stream = new MemoryStream(bytes))
                {
                    var target = await _serializer.DeserializeAsync(stream, type, CancellationToken.None);
                    return (Success: true, DeserializedObject: target);
                }
            }
            catch
            {
                return (Success: false, DeserializedObject: null);
            }
        }
    }
}
