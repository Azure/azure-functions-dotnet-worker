﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.Functions.Worker.E2EApp.BasicHttpFunctions;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public static class FailingHttpFunctions
    {
        [Function(nameof(ExceptionFunction))]
        public static HttpResponseData ExceptionFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(ExceptionFunction));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            throw new Exception("This should never succeed!");
        }        
        
        [Function(nameof(PocoWithoutBindingSource))]
        public static HttpResponseData PocoWithoutBindingSource(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] CallerName caller,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(PocoWithoutBindingSource));
            logger.LogInformation(".NET Worker HTTP trigger function processed a request");
            throw new Exception("This should never succeed!");
        }
    }
}
