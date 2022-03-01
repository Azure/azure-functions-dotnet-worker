// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core.Serialization;
using System.Threading;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// Provides extension methods to work with an <see cref="HttpRequestData"/> instance.
    /// </summary>
    public static class HttpRequestDataExtensions
    {
        /// <summary>
        /// Reads the body payload as a string.
        /// </summary>
        /// <param name="request">The request from which to read.</param>
        /// <param name="encoding">The encoding to use when reading the string. Defaults to UTF-8</param>
        /// <returns>A <see cref="Task{String}"/> that represents the asynchronous read operation.</returns>
        public static async Task<string?> ReadAsStringAsync(this HttpRequestData request, Encoding? encoding = null)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Body is null)
            {
                return null;
            }

            using (var reader = new StreamReader(request.Body, bufferSize: -1, detectEncodingFromByteOrderMarks: true, encoding: encoding, leaveOpen: true))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Reads the body payload as a string.
        /// </summary>
        /// <param name="request">The request from which to read.</param>
        /// <param name="encoding">The encoding to use when reading the string. Defaults to UTF-8</param>
        /// <returns>A <see cref="string"/> that represents request body.</returns>
        public static string? ReadAsString(this HttpRequestData request, Encoding? encoding = null)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Body is null)
            {
                return null;
            }

            using (var reader = new StreamReader(request.Body, bufferSize: -1, detectEncodingFromByteOrderMarks: true, encoding: encoding, leaveOpen: true))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Reads the request using the default <see cref="ObjectSerializer"/> configured for this worker.
        /// </summary>
        /// <typeparam name="T">The target type of the JSON value.</typeparam>
        /// <param name="request">The request to be read.</param>
        /// <param name="cancellationToken">A token that may be used to cancel the read operation.</param>
        /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation.</returns>
        public static ValueTask<T?> ReadFromJsonAsync<T>(this HttpRequestData request, CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ObjectSerializer serializer = request.FunctionContext.InstanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
                 ?? throw new InvalidOperationException("A serializer is not configured for the worker.");

            return ReadFromJsonAsync<T>(request, serializer, cancellationToken);
        }

        /// <summary>
        /// Reads the request using the provided <see cref="ObjectSerializer"/>.
        /// </summary>
        /// <typeparam name="T">The target type of the JSON value.</typeparam>
        /// <param name="request">The request to be read.</param>
        /// <param name="serializer">The <see cref="ObjectSerializer"/> to use for the deserialization.</param>
        /// <param name="cancellationToken">A token that may be used to cancel the read operation.</param>
        /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation.</returns>
        public static ValueTask<T?> ReadFromJsonAsync<T>(this HttpRequestData request, ObjectSerializer serializer, CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            ValueTask<object?> result = serializer.DeserializeAsync(request.Body, typeof(T), cancellationToken);

            static T? TryCast(object? value)
            {
                return value != null
                    ? (T)value
                    : default;
            }

            if (result.IsCompletedSuccessfully)
            {
                return new ValueTask<T?>(TryCast(result.Result));
            }

            return new ValueTask<T?>(result.AsTask().ContinueWith(t => TryCast(t.Result)));
        }


        /// <summary>
        /// Creates a response for the the provided <see cref="HttpRequestData"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestData"/> for this response.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <returns>The response data.</returns>
        public static HttpResponseData CreateResponse(this HttpRequestData request, HttpStatusCode statusCode)
        {
            var response = request.CreateResponse();
            response.StatusCode = statusCode;

            return response;
        }
    }
}
