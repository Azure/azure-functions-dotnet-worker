// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// Provides extension methods to work with an <see cref="HttpResponseData"/> instance.
    /// </summary>
    public static class HttpResponseDataExtensions
    {
        /// <summary>
        /// Writes the provided string to the response body using the specified encoding.
        /// </summary>
        /// <param name="response">The response to write the string to.</param>
        /// <param name="value">The string content to write to the request body.</param>
        /// <param name="encoding">The encoding to use when writing the string. Defaults to UTF-8</param>
        public static void WriteString(this HttpResponseData response, string value, Encoding? encoding = null)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            encoding ??= Encoding.UTF8;

            byte[] bytes = encoding.GetBytes(value);
            response.Body.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes the provided string to the response body using the specified encoding.
        /// </summary>
        /// <param name="response">The response to write the string to.</param>
        /// <param name="value">The string content to write to the request body.</param>
        /// <param name="encoding">The encoding to use when writing the string. Defaults to UTF-8</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task WriteStringAsync(this HttpResponseData response, string value, Encoding? encoding = null)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            encoding ??= Encoding.UTF8;

            byte[] bytes = encoding.GetBytes(value);
            return response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the default <see cref="ObjectSerializer"/> configured for this worker.
        /// The response content-type will be set to <code>application/json; charset=utf-8</code> and the status code set to 200.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, CancellationToken cancellationToken = default)
        {
            return WriteAsJsonAsync(response, instance, "application/json; charset=utf-8", HttpStatusCode.OK, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the default <see cref="ObjectSerializer"/> configured for this worker.
        /// The response content-type will be set to <code>application/json; charset=utf-8</code> and the status code set to the provided <paramref name="statusCode"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, HttpStatusCode statusCode,
            CancellationToken cancellationToken = default)
        {
            return WriteAsJsonAsync(response, instance, "application/json; charset=utf-8", statusCode, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the default <see cref="ObjectSerializer"/> configured for this worker.
        /// The response content-type will be set to the provided <paramref name="contentType"/> and the status code set to 200.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, string contentType, CancellationToken cancellationToken = default)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            ObjectSerializer serializer = GetObjectSerializer(response);

            return WriteAsJsonAsync(response, instance, serializer, contentType, HttpStatusCode.OK, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the default <see cref="ObjectSerializer"/> configured for this worker.
        /// The response content-type will be set to the provided <paramref name="contentType"/> and the status code set to the provided <paramref name="statusCode"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, string contentType, HttpStatusCode statusCode,
            CancellationToken cancellationToken = default)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            ObjectSerializer serializer = GetObjectSerializer(response);

            return WriteAsJsonAsync(response, instance, serializer, contentType, statusCode, cancellationToken);
        }


        /// <summary>
        /// Write the specified value as JSON to the response body using the provided <see cref="ObjectSerializer"/>.
        /// The response content-type will be set to <code>application/json; charset=utf-8</code> and the status code set to 200.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="serializer">The serializer used to serialize the instance.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, ObjectSerializer serializer, CancellationToken cancellationToken = default)
        {
            return WriteAsJsonAsync(response, instance, serializer, "application/json; charset=utf-8", HttpStatusCode.OK, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the provided <see cref="ObjectSerializer"/>.
        /// The response content-type will be set to <code>application/json; charset=utf-8</code> and the status code set to the provided <paramref name="statusCode"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="serializer">The serializer used to serialize the instance.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, ObjectSerializer serializer, HttpStatusCode statusCode,
            CancellationToken cancellationToken = default)
        {
            return WriteAsJsonAsync(response, instance, serializer, "application/json; charset=utf-8", statusCode, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the provided <see cref="ObjectSerializer"/>.
        /// The response content-type will be set to the provided <paramref name="contentType"/> and the status code set to 200.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="serializer">The serializer used to serialize the instance.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance,
            ObjectSerializer serializer, string contentType,
            CancellationToken cancellationToken = default)
        {
            return WriteAsJsonAsync(response, instance, serializer, contentType, HttpStatusCode.OK, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body using the provided <see cref="ObjectSerializer"/>.
        /// The response content-type will be set to the provided <paramref name="contentType"/> and the status code set to the provided <paramref name="statusCode"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="instance">The instance to serialize and write as JSON.</param>
        /// <param name="serializer">The serializer used to serialize the instance.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        public static ValueTask WriteAsJsonAsync<T>(this HttpResponseData response, T instance, ObjectSerializer serializer, string contentType, HttpStatusCode statusCode, CancellationToken cancellationToken = default)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (contentType is null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            response.Headers.Add("Content-Type", contentType);
            response.StatusCode = statusCode;

            return serializer.SerializeAsync(response.Body, instance, typeof(T), cancellationToken);
        }

        /// <summary>
        /// Writes the provided bytes to the response body.
        /// </summary>
        /// <param name="response">The response to write the string to.</param>
        /// <param name="value">The byte content to write to the request body.</param>
        public static void WriteBytes(this HttpResponseData response, byte[] value)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            response.Body.Write(value, 0, value.Length);
        }

        /// <summary>
        /// Writes the provided bytes to the response body.
        /// </summary>
        /// <param name="response">The response to write the string to.</param>
        /// <param name="value">The byte content to write to the request body.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task WriteBytesAsync(this HttpResponseData response, byte[] value)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return response.Body.WriteAsync(value, 0, value.Length);
        }

        private static ObjectSerializer GetObjectSerializer(HttpResponseData response)
        {
            return response.FunctionContext.InstanceServices.GetService<IOptions<WorkerOptions>>()?.Value?.Serializer
                ?? throw new InvalidOperationException("A serializer is not configured for the worker.");
        }
    }
}
