// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Http.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    internal class DefaultFromBodyConversionHandler : IFromBodyConversionFeature
    {
        public static IFromBodyConversionFeature Instance { get; } = new DefaultFromBodyConversionHandler();

        public ValueTask<object?> ConvertAsync(FunctionContext context, Type targetType)
        {
            var requestDataResult = context.GetHttpRequestDataAsync();

            if (requestDataResult.IsCompletedSuccessfully)
            {
                return ConvertRequestAsync(requestDataResult.Result, context, targetType);
            }

            return ConvertAsync(requestDataResult, context, targetType);
        }

        private async ValueTask<object?> ConvertAsync(ValueTask<HttpRequestData?> requestDataResult, FunctionContext context, Type targetType)
        {
            var requestData = await requestDataResult;
            return await ConvertRequestAsync(requestData, context, targetType);
        }

        private ValueTask<object?> ConvertRequestAsync(HttpRequestData? requestData, FunctionContext context, Type targetType)
        {
            if (requestData is null)
            {
               throw new InvalidOperationException($"The '{nameof(DefaultFromBodyConversionHandler)} expects an '{nameof(HttpRequestData)}' instance in the current context.");
            }

            return ConvertBodyAsync(requestData, context, targetType);
        }

        private static ValueTask<object?> ConvertBodyAsync(HttpRequestData requestData, FunctionContext context, Type targetType)
        {
            object? result;
            if (targetType == typeof(string))
            {
                result = requestData.ReadAsString();
            }
            else if (targetType == typeof(byte[]))
            {
                result = ReadBytes(requestData, context.CancellationToken);
            }
            else if (targetType == typeof(Memory<byte>))
            {
                Memory<byte> bytes = ReadBytes(requestData, context.CancellationToken);
                result = bytes;
            }
            else if (HasJsonContentType(requestData))
            {
                ObjectSerializer serializer = requestData.FunctionContext.InstanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
                 ?? throw new InvalidOperationException("A serializer is not configured for the worker.");

                result = serializer.Deserialize(requestData.Body, targetType, context.CancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"The type '{targetType}' is not supported by the '{nameof(DefaultFromBodyConversionHandler)}'.");
            }

            return new ValueTask<object?>(result);
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
