// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
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
                return ConversionResult.Unhandled();
            }

            var bytes = context.Source switch
            {
                string sourceString => Encoding.UTF8.GetBytes(sourceString),
                ReadOnlyMemory<byte> sourceMemory => sourceMemory.ToArray(),
                HttpRequestData requestData => Encoding.UTF8.GetBytes(new StreamReader(requestData.Body).ReadToEnd()),
                _ => null
            };

            if (bytes == null)
            {
                return ConversionResult.Unhandled();
            }

            return await GetConversionResultFromDeserialization(bytes, context.TargetType);
        }

        private async Task<ConversionResult> GetConversionResultFromDeserialization(byte[] bytes, Type type)
        {
            Stream? stream = null;

            try
            {
                stream = new MemoryStream(bytes);

                var deserializedObject = await _serializer.DeserializeAsync(stream, type, CancellationToken.None);
                return ConversionResult.Success(deserializedObject);

            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
            finally
            {
                if (stream != null) 
                {
#if NET5_0_OR_GREATER

                    await ((IAsyncDisposable)stream).DisposeAsync();
#else
                    ((IDisposable)stream).Dispose();
#endif
                }
            }
        }
    }
}
