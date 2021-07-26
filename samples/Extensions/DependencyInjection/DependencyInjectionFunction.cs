// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class DependencyInjectionFunction
    {
        private readonly IHttpResponderService _responderService;
        private readonly ILogger<DependencyInjectionFunction> _logger;

        public DependencyInjectionFunction(IHttpResponderService responderService, ILogger<DependencyInjectionFunction> logger)
        {
            _responderService = responderService;
            _logger = logger;
        }

        [Function(nameof(DependencyInjectionFunction))]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("message logged");

            return _responderService.ProcessRequest(req);
        }
    }

    public interface IHttpResponderService
    {
        HttpResponseData ProcessRequest(HttpRequestData httpRequest);
    }

    public class DefaultHttpResponderService : IHttpResponderService
    {
        public DefaultHttpResponderService()
        {

        }

        public HttpResponseData ProcessRequest(HttpRequestData httpRequest)
        {
            var response = httpRequest.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");
            response.WriteString("Welcome to .NET 5!!");

            return response;
        }
    }
}
