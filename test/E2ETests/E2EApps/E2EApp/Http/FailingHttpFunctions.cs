// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

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
    }
}
