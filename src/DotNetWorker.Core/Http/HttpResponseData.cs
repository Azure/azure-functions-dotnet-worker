// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Net;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// A representation of the outgoing HTTP response.
    /// </summary>
    public abstract class HttpResponseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseData"/> class.
        /// </summary>
        /// <param name="functionContext">The <see cref="FunctionContext"/> for this response.</param>
        public HttpResponseData(FunctionContext functionContext)
        {
            FunctionContext = functionContext ?? throw new System.ArgumentNullException(nameof(functionContext));
        }

        /// <summary>
        /// Gets or sets the status code for the response.
        /// </summary>
        public abstract HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="HttpHeadersCollection"/> containing the response headers
        /// </summary>
        public abstract HttpHeadersCollection Headers { get; set; }

        /// <summary>
        /// Gets or sets the response body stream.
        /// </summary>
        public abstract Stream Body { get; set; }

        /// <summary>
        /// Gets an <see cref="HttpCookies"/> instance containing the request cookies.
        /// </summary>
        public abstract HttpCookies Cookies { get; }

        /// <summary>
        /// Gets the <see cref="FunctionContext"/> for this response.
        /// </summary>
        public FunctionContext FunctionContext { get; }

        /// <summary>
        /// Creates an HTTP response for the provided request.
        /// </summary>
        /// <param name="request">The request for which we need to create a response.</param>
        /// <returns>An <see cref="HttpResponseData"/> that represens the response for the provided request.</returns>
        public static HttpResponseData CreateResponse(HttpRequestData request)
        {
            if (request is null)
            {
                throw new System.ArgumentNullException(nameof(request));
            }

            return request.CreateResponse();
        }
    }
}
