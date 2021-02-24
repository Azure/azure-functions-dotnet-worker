// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        /// <returns>A <see cref="Task{string?}"/> that represents the asynchronous read operation.</returns>
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

            using (var reader = new StreamReader(request.Body, encoding: encoding, leaveOpen: true))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Reads the body payload as a string.
        /// </summary>
        /// <param name="request">The request from which to read.</param>
        /// <returns>A <see cref="Task{String?}"/> that represents the asynchronous read operation.</returns>
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

            using (var reader = new StreamReader(request.Body, encoding: encoding, leaveOpen: true))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Creates a response for the the provided <see cref="HttpRequestData"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestData"/> for this response.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <returns></returns>
        public static HttpResponseData CreateResponse(this HttpRequestData request, HttpStatusCode statusCode)
        {
            var response = request.CreateResponse();
            response.StatusCode = statusCode;

            return response;
        }
    }
}
