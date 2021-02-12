// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Http
{
    public static class HttpRequestDataExtensions
    {
        /// <summary>
        /// Reads the body payload as a string.
        /// </summary>
        /// <param name="request">The request from which to read.</param>
        /// <returns>The body content as a string, or null if the request body property is null.</returns>
        public static string? ReadAsString(this HttpRequestData request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Body is null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(request.Body.Value.Span);
        }
    }
}
