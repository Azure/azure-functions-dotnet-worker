// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace FunctionApp
{
    public static class Function3
    {

        [FunctionName("Function3")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [Queue("functionstesting2", Connection = "AzureWebJobsStorage")] OutputBinding<string> name)
        {
            var response = new HttpResponseData(HttpStatusCode.OK);
            response.Body = "Success!!";

            name.SetValue("some name");

            return response;
        }
    }

}
