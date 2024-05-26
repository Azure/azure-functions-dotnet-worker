// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace AspNetIntegration
{
    public class SimpleHttpTrigger
    {
        //<docsnippet_aspnet_http_trigger>
        [Function("SimpleHttpTrigger")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            return new OkObjectResult("Welcome to Azure Functions!");
        }
        //</docsnippet_aspnet_http_trigger>
    }

    public class SimpleHttpTriggerHttpContext
    {
        //<docsnippet_aspnet_http_trigger_http_context>
        [Function("SimpleHttpTriggerHttpContext")]
        public async Task RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpContext httpContext)
        {
            static int WriteBody(PipeWriter writer)
            {
                ReadOnlySpan<byte> body = "Welcome to Azure Functions!"u8;
                writer.Write(body);
                return body.Length;
            }

            var responseHeaders = httpContext.Response.GetTypedHeaders();
            responseHeaders.ContentLength = WriteBody(httpContext.Response.BodyWriter);
            responseHeaders.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);
            await httpContext.Response.BodyWriter.FlushAsync();
        }
        //</docsnippet_aspnet_http_trigger_http_context>
    }

    public class SimpleHttpTriggerHttpData
    {
        [Function("SimpleHttpTriggerHttpData")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            await response.WriteStringAsync("Welcome to Azure Functions (HttpData)");

            return response;
        }
    }
}
