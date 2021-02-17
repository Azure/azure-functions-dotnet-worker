// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Http
{
    public static class HttpRequestDataExtensions
    {
        /// <summary>
        /// Reads the body payload as a string.
        /// </summary>
        /// <param name="request">The request from which to read.</param>
        /// <returns>A <see cref="Task{String?}"/> that represents the asynchronous read operation.</returns>
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

        public static HttpResponseData CreateResponse(this HttpRequestData request, HttpStatusCode statusCode)
        {
            var response = request.CreateResponse();
            response.StatusCode = statusCode;

            return response;
        }
    }
}
