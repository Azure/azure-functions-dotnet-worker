// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public static class Function3
    {
        [Function("Function3")]
        public static string Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var headers = req.Headers;
            var logger = context.GetLogger("FunctionApp.Function3");

            var cookies = req.Cookies;
            cookies.ToList().ForEach(c => logger.LogInformation(c.Name));
            
            if(cookies.Count > 0)
            {
                return cookies.First().Name.ToString();
            }

            return "No cookies test";
        }
    }
}
