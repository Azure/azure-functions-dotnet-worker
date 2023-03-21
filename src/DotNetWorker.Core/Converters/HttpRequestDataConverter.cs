// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Microsoft.Azure.Functions.Worker.Core.Converters
{
    internal class HttpRequestDataConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            var requestDataResult = context.FunctionContext.GetHttpRequestDataAsync();

            if (requestDataResult.IsCompletedSuccessfully)
            {
                return ConvertRequestAsync(requestDataResult.Result, context);
            }

            return ConvertAsync(requestDataResult, context);
        }

        private async ValueTask<ConversionResult> ConvertAsync(ValueTask<HttpRequestData?> requestDataResult, ConverterContext context)
        {
            var requestData = await requestDataResult;
            return await ConvertRequestAsync(requestData, context);
        }

        private ValueTask<ConversionResult> ConvertRequestAsync(HttpRequestData? requestData, ConverterContext context)
        {
            if (requestData is null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            try
            {
                return ConvertBodyAsync(requestData, context);
            }
            catch (Exception ex)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
            }
        }

        private static ValueTask<ConversionResult> ConvertBodyAsync(HttpRequestData requestData, ConverterContext context)
        {
            object? result;
            if (context.TargetType == typeof(string))
            {
                result = requestData.ReadAsString();
            }
            else if (context.TargetType == typeof(byte[]))
            {
                result = ReadBytes(requestData, context.FunctionContext.CancellationToken);
            }
            else if (context.TargetType == typeof(Memory<byte>))
            {
                Memory<byte> bytes = ReadBytes(requestData, context.FunctionContext.CancellationToken);
                result = bytes;
            }
            else if (HasJsonContentType(requestData))
            {
                ObjectSerializer serializer = requestData.FunctionContext.InstanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
                 ?? throw new InvalidOperationException("A serializer is not configured for the worker.");

                result = serializer.Deserialize(requestData.Body, context.TargetType, context.FunctionContext.CancellationToken);
            }
            else
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            return new ValueTask<ConversionResult>(ConversionResult.Success(result));
        }

        private static byte[] ReadBytes(HttpRequestData requestData, CancellationToken cancellationToken)
        {
            var bytes = new byte[requestData.Body.Length];
            requestData.Body.Read(bytes, 0, bytes.Length);

            return bytes;
        }

        private static bool HasJsonContentType(HttpRequestData request)
        {
            var (key, value) = request.Headers
                .FirstOrDefault(h => string.Equals(h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase));

            if (value is not null
                && MediaTypeHeaderValue.TryParse(value.FirstOrDefault(), out var mediaType)
                && mediaType.MediaType != null)
            {

                // If the content type is application/json or +json (e.g., application/dl+json)
                if (string.Equals(mediaType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)
                        || mediaType.MediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
