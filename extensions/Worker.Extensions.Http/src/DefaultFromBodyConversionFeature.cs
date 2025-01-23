// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Http.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    internal class DefaultFromBodyConversionFeature : IFromBodyConversionFeature
    {
        internal static IFromBodyConversionFeature Instance { get; } = new DefaultFromBodyConversionFeature();

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
               throw new InvalidOperationException($"The '{nameof(DefaultFromBodyConversionFeature)} expects an '{nameof(HttpRequestData)}' instance in the current context.");
            }

            return ConvertBodyAsync(requestData, context, targetType);
        }

        private static async ValueTask<object?> ConvertBodyAsync(HttpRequestData requestData, FunctionContext context, Type targetType) => targetType switch
        {
            _ when targetType == typeof(string) => await requestData.ReadAsStringAsync(),
            _ when targetType == typeof(byte[]) => await ReadBytesAsync(requestData),
            _ when targetType == typeof(Memory<byte>) => new Memory<byte>(await ReadBytesAsync(requestData)),
            _ when HasJsonContentType(requestData) =>
                    await (requestData.FunctionContext.InstanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
                            ?? throw new InvalidOperationException("A serializer is not configured for the worker."))
                        .DeserializeAsync(requestData.Body, targetType, CancellationToken.None),
            _ => throw new InvalidOperationException($"The type '{targetType}' is not supported by the '{nameof(DefaultFromBodyConversionFeature)}'.")
        };

        private static async Task<byte[]> ReadBytesAsync(HttpRequestData requestData)
        {
            using var memoryStream = new MemoryStream();
            await requestData.Body.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
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
