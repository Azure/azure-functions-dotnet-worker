// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

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
