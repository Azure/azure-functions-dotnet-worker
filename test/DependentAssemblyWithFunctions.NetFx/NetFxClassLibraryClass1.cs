// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DependentAssemblyWithFunctions.NetFx
{
    public sealed class NetFxClassLibraryClass1
    {
        private readonly ILogger _logger;

        public NetFxClassLibraryClass1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NetFxClassLibraryClass1>();
        }

        [Function("NetFxClassLibraryClass1Function1")]
        public HttpResponseData Run1([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("NetFxClassLibraryClass1Function1");
            throw new NotImplementedException();
        }

        [Function("NetFxClassLibraryClass1Function2Async")]
        public Task<HttpResponseData> Run2([HttpTrigger(AuthorizationLevel.Admin, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("NetFxClassLibraryClass1Function2Async");
            throw new NotImplementedException();
        }
    }
}
